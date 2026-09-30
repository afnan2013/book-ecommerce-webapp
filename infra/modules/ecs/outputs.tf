output "cluster_name" {
  value = aws_ecs_cluster.main.name
}

output "service_name" {
  value = aws_ecs_service.api.name
}

output "task_definition_family" {
  value = aws_ecs_task_definition.api.family
}

output "container_name" {
  value = local.container_name
}

output "cluster_arn" {
  value = aws_ecs_cluster.main.arn
}

output "service_arn" {
  value = aws_ecs_service.api.id
}

output "task_definition_family_arn" {
  value = aws_ecs_task_definition.api.arn_without_revision
}

output "execution_role_arn" {
  value = aws_iam_role.execution.arn
}

output "task_role_arn" {
  value = aws_iam_role.task.arn
}

output "log_group_arn" {
  value = aws_cloudwatch_log_group.api.arn
}
