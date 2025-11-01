CREATE TABLE IF NOT EXISTS plans(
    id uuid PRIMARY KEY NOT NULL,
    chat_id bigint NOT NULL,
    name varchar NULL,
    from_message_id bigint NOT NULL,
    from_user_id bigint NOT NULL,
    create_date timestamptz NOT NULL,
    from_user_name varchar NULL,
    timestamp bigint NULL,
    cron varchar NOT NULL,
    cron_description varchar NOT NULL
);

CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_plans_chat_id_name ON plans (chat_id, name);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_plans_timestamp ON plans (timestamp);
ALTER TABLE plans ADD COLUMN IF NOT EXISTS int_id SERIAL;
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_plans_int_id ON plans (int_id);
ALTER TABLE plans ADD COLUMN IF NOT EXISTS cron_source varchar NOT NULL DEFAULT('');

CREATE TABLE IF NOT EXISTS chat_settings(
    chat_id bigint PRIMARY KEY NOT NULL,
    timestamp bigint NULL,
    settings varchar NOT NULL
)