ALTER TABLE users
    DROP COLUMN full_name,
    DROP COLUMN user_type,
    DROP COLUMN concurrency_stamp;

ALTER TABLE users ALTER COLUMN id DROP DEFAULT;
ALTER TABLE users ALTER COLUMN id TYPE BIGINT USING (row_number() OVER ())::bigint;
CREATE SEQUENCE users_id_seq OWNED BY users.id;
SELECT setval('users_id_seq', COALESCE((SELECT MAX(id) FROM users), 1));
ALTER TABLE users ALTER COLUMN id SET DEFAULT nextval('users_id_seq');
