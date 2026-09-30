output "github_actions_role_arn" {
  value = module.github_oidc.role_arn
}

output "api_url" {
  value = "https://${var.api_domain}"
}
