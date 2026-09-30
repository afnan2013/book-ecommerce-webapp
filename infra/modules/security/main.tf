resource "aws_security_group" "alb" {
  name        = "${var.name_prefix}-alb-sg"
  description = "Public entry: HTTP and HTTPS from the internet"
  vpc_id      = var.vpc_id

  tags = { Name = "${var.name_prefix}-alb-sg" }
}

resource "aws_security_group" "task" {
  name        = "${var.name_prefix}-task-sg"
  description = "App containers: traffic only from the ALB"
  vpc_id      = var.vpc_id

  tags = { Name = "${var.name_prefix}-task-sg" }
}

resource "aws_security_group" "rds" {
  name        = "${var.name_prefix}-rds-sg"
  description = "Database: traffic only from app containers"
  vpc_id      = var.vpc_id

  tags = { Name = "${var.name_prefix}-rds-sg" }
}

# ALB: anyone on the internet may connect on 80 and 443
resource "aws_vpc_security_group_ingress_rule" "alb_http" {
  security_group_id = aws_security_group.alb.id
  cidr_ipv4         = "0.0.0.0/0"
  from_port         = 80
  to_port           = 80
  ip_protocol       = "tcp"
}

resource "aws_vpc_security_group_ingress_rule" "alb_https" {
  security_group_id = aws_security_group.alb.id
  cidr_ipv4         = "0.0.0.0/0"
  from_port         = 443
  to_port           = 443
  ip_protocol       = "tcp"
}

# ALB may only send traffic to the app containers
resource "aws_vpc_security_group_egress_rule" "alb_to_task" {
  security_group_id            = aws_security_group.alb.id
  referenced_security_group_id = aws_security_group.task.id
  from_port                    = var.app_port
  to_port                      = var.app_port
  ip_protocol                  = "tcp"
}

# Task: only the ALB may connect
resource "aws_vpc_security_group_ingress_rule" "task_from_alb" {
  security_group_id            = aws_security_group.task.id
  referenced_security_group_id = aws_security_group.alb.id
  from_port                    = var.app_port
  to_port                      = var.app_port
  ip_protocol                  = "tcp"
}

# Task may reach anything: ECR, CloudWatch Logs, SSM, RDS
resource "aws_vpc_security_group_egress_rule" "task_all" {
  security_group_id = aws_security_group.task.id
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
}

# RDS: only the app containers may connect
resource "aws_vpc_security_group_ingress_rule" "rds_from_task" {
  security_group_id            = aws_security_group.rds.id
  referenced_security_group_id = aws_security_group.task.id
  from_port                    = var.db_port
  to_port                      = var.db_port
  ip_protocol                  = "tcp"
}