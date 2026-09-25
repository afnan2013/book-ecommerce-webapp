package main

import (
	"errors"
	"log"
	"os"

	"github.com/golang-migrate/migrate/v4"
	_ "github.com/golang-migrate/migrate/v4/database/postgres"
	_ "github.com/golang-migrate/migrate/v4/source/file"
	"github.com/joho/godotenv"
)

func main() {
	if err := godotenv.Load(); err != nil {
		log.Println("no .env file found, relying on real environment variables")
	}

	if len(os.Args) < 2 {
		log.Fatal("usage: go run ./cmd/migrate [up|down]")
	}

	m, err := migrate.New("file://migrations", os.Getenv("DATABASE_URL"))
	if err != nil {
		log.Fatalf("failed to initialize migrate: %v", err)
	}

	switch os.Args[1] {
	case "up":
		err = m.Up()
	case "down":
		err = m.Down()
	default:
		log.Fatalf("unknown command %q, want up or down", os.Args[1])
	}

	if err != nil && !errors.Is(err, migrate.ErrNoChange) {
		log.Fatalf("migration failed: %v", err)
	}

	log.Println("migration complete")
}
