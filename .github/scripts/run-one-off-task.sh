#!/usr/bin/env bash
# Runs a task to completion inside the service's network, prints its logs, and exits with its status.
# The task borrows the service's subnets and security group, which is what lets it reach the private database.
# Usage: run-one-off-task.sh <task-definition-arn> <container-name> [command-json]
# Requires ECS_CLUSTER and ECS_SERVICE in the environment.
set -euo pipefail

task_definition="$1"
container="$2"
command_json="${3:-}"

network=$(aws ecs describe-services --cluster "$ECS_CLUSTER" --services "$ECS_SERVICE" \
  --query 'services[0].networkConfiguration' --output json)

args=()
if [ -n "$command_json" ]; then
  args+=(--overrides "$(jq -nc --arg name "$container" --argjson cmd "$command_json" \
    '{containerOverrides: [{name: $name, command: $cmd}]}')")
fi

task_arn=$(aws ecs run-task \
  --cluster "$ECS_CLUSTER" \
  --task-definition "$task_definition" \
  --launch-type FARGATE \
  --network-configuration "$network" \
  --started-by "github-actions-${GITHUB_RUN_ID:-local}" \
  "${args[@]}" \
  --query 'tasks[0].taskArn' --output text)

if [ -z "$task_arn" ] || [ "$task_arn" = "None" ]; then
  echo "::error::Task failed to start"
  exit 1
fi

echo "Started $task_arn"
aws ecs wait tasks-stopped --cluster "$ECS_CLUSTER" --tasks "$task_arn"

log_options=$(aws ecs describe-task-definition --task-definition "$task_definition" \
  --query "taskDefinition.containerDefinitions[?name=='$container'].logConfiguration.options | [0]" \
  --output json)
log_group=$(jq -r '.["awslogs-group"]' <<< "$log_options")
log_prefix=$(jq -r '.["awslogs-stream-prefix"]' <<< "$log_options")

echo "── $container logs ──"
aws logs get-log-events \
  --log-group-name "$log_group" \
  --log-stream-name "$log_prefix/$container/${task_arn##*/}" \
  --query 'events[].message' --output text || echo "(no logs found)"

exit_code=$(aws ecs describe-tasks --cluster "$ECS_CLUSTER" --tasks "$task_arn" \
  --query "tasks[0].containers[?name=='$container'].exitCode | [0]" --output text)

if [ "$exit_code" != "0" ]; then
  reason=$(aws ecs describe-tasks --cluster "$ECS_CLUSTER" --tasks "$task_arn" \
    --query 'tasks[0].stoppedReason' --output text)
  echo "::error::$container task failed (exit code $exit_code): $reason"
  exit 1
fi
