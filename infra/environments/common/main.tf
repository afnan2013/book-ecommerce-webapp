locals {
  app_port = 8080
  tags = {
    Project   = var.project
    Env       = var.env
    ManagedBy = "terraform"
  }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = local.tags
  }
}

provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"

  default_tags {
    tags = local.tags
  }
}

module "network" {
  source = "../../modules/network"

  name_prefix     = var.name_prefix
  vpc_cidr        = var.vpc_cidr
  public_subnets  = var.public_subnets
  private_subnets = var.private_subnets
}

module "security" {
  source = "../../modules/security"

  name_prefix = var.name_prefix
  vpc_id      = module.network.vpc_id
  app_port    = local.app_port
  db_port     = 5432
}

module "ecr" {
  source = "../../modules/ecr"

  repository_name = "${var.name_prefix}-api"
  images_to_keep  = 10
}

module "database" {
  source = "../../modules/database"

  name_prefix       = var.name_prefix
  subnet_ids        = module.network.private_subnet_ids
  security_group_id = module.security.rds_sg_id

  engine_version    = "17"
  instance_class    = var.db_instance_class
  allocated_storage = var.db_allocated_storage
  db_name           = "bookstore"
  username          = "be"

  backup_retention_days = var.db_backup_retention_days
  skip_final_snapshot   = var.db_skip_final_snapshot
  deletion_protection   = var.db_deletion_protection
}

data "aws_route53_zone" "main" {
  name = var.dns_zone_name
}

module "api_certificate" {
  source = "../../modules/certificate"

  domain_name = var.api_domain
  zone_id     = data.aws_route53_zone.main.zone_id
}

module "alb" {
  source = "../../modules/alb"

  name_prefix = var.name_prefix
  vpc_id = module.network.vpc_id
  subnet_ids = module.network.public_subnet_ids
  security_group_id = module.security.alb_sg_id
  certificate_arn = module.api_certificate.certificate_arn
  app_port = local.app_port
  health_check_path = "/health"
}

module "ecs" {
  source = "../../modules/ecs"

  name_prefix       = var.name_prefix
  region            = var.region
  subnet_ids        = module.network.public_subnet_ids
  security_group_id = module.security.task_sg_id
  target_group_arn  = module.alb.target_group_arn

  repository_url = module.ecr.repository_url
  image_tag      = "bootstrap"
  app_port       = local.app_port

  cpu                = var.api_cpu
  memory             = var.api_memory
  desired_count      = var.api_desired_count
  log_retention_days = var.log_retention_days

  cors_allowed_origin  = "https://${var.web_domain}"
  database_url_ssm_arn = module.database.database_url_ssm_arn

  depends_on = [module.alb]
}