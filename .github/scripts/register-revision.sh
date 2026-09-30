#!/usr/bin/env bash
# Registers a new revision of a task definition family with one container's image swapped.
# Starting from the live revision keeps whatever Terraform last set (env vars, secrets, sizes).
# Usage: register-revision.sh <family> <container-name> <image>
# Prints the new revision ARN.
set -euo pipefail

family="$1"
container="$2"
image="$3"

aws ecs describe-task-definition --task-definition "$family" --query taskDefinition --output json |
  jq --arg image "$image" --arg name "$container" '
    .containerDefinitions |= map(if .name == $name then .image = $image else . end)
    | del(.taskDefinitionArn, .revision, .status, .requiresAttributes,
          .compatibilities, .registeredAt, .registeredBy)
  ' > "taskdef-$family.json"

aws ecs register-task-definition --cli-input-json "file://taskdef-$family.json" \
  --query taskDefinition.taskDefinitionArn --output text
