variable "name_prefix" {
  type = string
}

variable "github_repository" {
  type = string
}

variable "github_environment" {
  type = string
}

variable "ecr_repository_arn" {
  type = string
}

variable "ecs_cluster_arn" {
  type = string
}

variable "ecs_service_arn" {
  type = string
}

variable "task_definition_family_arn" {
  type = string
}

variable "pass_role_arns" {
  type = list(string)
}

variable "log_group_arn" {
  type = string
}
