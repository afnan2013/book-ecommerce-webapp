package main

import (
	"bufio"
	"context"
	"errors"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/joho/godotenv"
	"golang.org/x/crypto/bcrypt"
)

const superAdminRoleName = "SuperAdmin"

func main() {
	if err := godotenv.Load(); err != nil {
		log.Println("no .env file found, relying on real environment variables")
	}

	reader := bufio.NewReader(os.Stdin)

	fmt.Print("Super admin email: ")
	email, _ := reader.ReadString('\n')
	email = strings.TrimSpace(email)

	fmt.Print("Super admin password: ")
	password, _ := reader.ReadString('\n')
	password = strings.TrimSpace(password)

	if email == "" {
		log.Fatal("email is required")
	}
	if len(password) < 8 {
		log.Fatal("password must be at least 8 characters")
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	pool, err := server.NewDBPool(ctx, os.Getenv("DATABASE_URL"))
	if err != nil {
		log.Fatalf("failed to connect to database: %v", err)
	}
	defer pool.Close()

	userID, err := findOrCreateUser(ctx, pool, email, password)
	if err != nil {
		log.Fatalf("failed to find or create user: %v", err)
	}

	roleID, err := getSuperAdminRoleID(ctx, pool)
	if err != nil {
		log.Fatalf("failed to look up %s role: %v (has the api server been started at least once, so the role gets synced?)", superAdminRoleName, err)
	}

	if _, err := pool.Exec(ctx, `
        INSERT INTO user_roles (user_id, role_id)
        VALUES ($1, $2)
        ON CONFLICT DO NOTHING
    `, userID, roleID); err != nil {
		log.Fatalf("failed to attach %s role to user: %v", superAdminRoleName, err)
	}

	log.Printf("%s is now a %s", email, superAdminRoleName)
}

func findOrCreateUser(ctx context.Context, pool server.DBQuerier, email, password string) (uuid.UUID, error) {
	var userID uuid.UUID

	err := pool.QueryRow(ctx, "SELECT id FROM users WHERE email = $1", email).Scan(&userID)
	if err == nil {
		log.Printf("user %s already exists, reusing existing account", email)
		return userID, nil
	}
	if !errors.Is(err, pgx.ErrNoRows) {
		return uuid.UUID{}, fmt.Errorf("looking up user by email: %w", err)
	}

	passwordHash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return uuid.UUID{}, fmt.Errorf("hashing password: %w", err)
	}

	err = pool.QueryRow(ctx, `
        INSERT INTO users (email, password_hash, full_name, user_type, concurrency_stamp)
        VALUES ($1, $2, $3, $4, $5)
        RETURNING id
    `, email, string(passwordHash), "Super Admin", 1, uuid.NewString()).Scan(&userID)
	if err != nil {
		return uuid.UUID{}, fmt.Errorf("creating user: %w", err)
	}

	log.Printf("created user %s", email)
	return userID, nil
}

func getSuperAdminRoleID(ctx context.Context, pool server.DBQuerier) (uuid.UUID, error) {
	var roleID uuid.UUID
	err := pool.QueryRow(ctx, "SELECT id FROM roles WHERE name = $1", superAdminRoleName).Scan(&roleID)
	if err != nil {
		return uuid.UUID{}, err
	}
	return roleID, nil
}
