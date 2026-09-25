CREATE TABLE books (
    id     BIGSERIAL PRIMARY KEY,
    title  TEXT NOT NULL,
    author TEXT NOT NULL,
    price  NUMERIC(10, 2) NOT NULL CHECK (price > 0)
);