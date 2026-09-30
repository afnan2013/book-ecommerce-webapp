name_prefix = "be-dev-1"
env         = "dev"
region      = "ap-south-1"

vpc_cidr = "10.0.0.0/16"

public_subnets = {
  a = { cidr = "10.0.1.0/24", az = "ap-south-1a" }
  b = { cidr = "10.0.2.0/24", az = "ap-south-1b" }
}

private_subnets = {
  a = { cidr = "10.0.11.0/24", az = "ap-south-1a" }
  b = { cidr = "10.0.12.0/24", az = "ap-south-1b" }
}

db_instance_class        = "db.t4g.micro"
db_allocated_storage     = 20
db_backup_retention_days = 0
db_skip_final_snapshot   = true
db_deletion_protection   = false

dns_zone_name = "afnanio.top"
api_domain    = "api.bookstore.dev.afnanio.top"

api_cpu            = 256
api_memory         = 512
api_desired_count  = 0
log_retention_days = 3

web_domain = "bookstore.dev.afnanio.top"

superadmin_email = "admin@afnanio.top"
