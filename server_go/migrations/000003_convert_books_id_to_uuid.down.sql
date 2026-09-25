ALTER TABLE books ALTER COLUMN id DROP DEFAULT;
ALTER TABLE books ALTER COLUMN id TYPE BIGINT USING (row_number() OVER ())::bigint;
CREATE SEQUENCE books_id_seq OWNED BY books.id;
SELECT setval('books_id_seq', COALESCE((SELECT MAX(id) FROM books), 1));
ALTER TABLE books ALTER COLUMN id SET DEFAULT nextval('books_id_seq');
