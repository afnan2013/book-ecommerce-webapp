locals {
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
  vpc_id = module.network.vpc_id
  app_port = 8080
  db_port = 5432
}

module "ecr" {
  source = "../../modules/ecr"

  repository_name = "${var.name_prefix}-api"
  images_to_keep = 10
}