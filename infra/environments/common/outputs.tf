output "github_actions_role_arn" {
  value = module.github_oidc.role_arn
}

output "api_url" {
  value = "https://${var.api_domain}"
}

output "superadmin_password_parameter" {
  value = module.ecs.superadmin_password_ssm_name
}

output "web_url" {
  value = "https://${var.web_domain}"
}

output "web_bucket" {
  value = module.web.bucket_name
}

output "cloudfront_distribution_id" {
  value = module.web.distribution_id
}
