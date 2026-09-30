output "database_url_ssm_arn" {
  value = aws_ssm_parameter.database_url.arn
}

output "endpoint" {
  value = aws_db_instance.main.endpoint
}