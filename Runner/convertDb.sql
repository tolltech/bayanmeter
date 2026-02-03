CREATE TABLE IF NOT EXISTS
    messages
(
    str_id varchar not null primary key,
    chat_id bigint not null,
    text varchar null,
    forward_from_chat_name varchar null,
    edit_date timestamp not null,
    forward_from_message_id int not null,
    from_user_id int not null,
    forward_from_user_id int null,
    create_date timestamp not null,
    forward_from_user_name varchar null,
    forward_from_chat_id bigint null,
    from_user_name varchar null,
    int_id int not null,
    message_date timestamp not null,
    timestamp bigint not null,
    hash varchar not null
);

CREATE INDEX IF NOT EXISTS messages_chat_id on messages (chat_id);
CREATE INDEX IF NOT EXISTS messages_from_user_id on messages (from_user_id);
CREATE INDEX IF NOT EXISTS messages_from_user_name on messages (from_user_name);
CREATE INDEX IF NOT EXISTS messages_int_id on messages (int_id);
CREATE INDEX IF NOT EXISTS messages_message_date on messages (message_date);
CREATE INDEX IF NOT EXISTS messages_chat_id_message_date on messages (chat_id, message_date);
CREATE INDEX IF NOT EXISTS messages_timestamp on messages (timestamp);

ALTER TABLE messages ADD COLUMN IF NOT EXISTS bayan_count int default 0;
ALTER TABLE messages ADD COLUMN IF NOT EXISTS previous_message_id int null;

ALTER TABLE messages ALTER COLUMN edit_date DROP NOT NULL;
--ALTER TABLE ImportResults ADD COLUMN IF NOT EXISTS candidatealbumid varchar NULL;

ALTER TABLE messages ALTER COLUMN forward_from_message_id TYPE BIGINT;
ALTER TABLE messages ALTER COLUMN from_user_id TYPE BIGINT;
ALTER TABLE messages ALTER COLUMN forward_from_user_id TYPE BIGINT;
ALTER TABLE messages ALTER COLUMN forward_from_user_id DROP NOT NULL;

CREATE TABLE IF NOT EXISTS
    moira_alerts
(
    str_id varchar not null primary key,
    chat_id bigint not null,
    text varchar null,
    int_id int not null,
    message_date timestamp not null,
    timestamp bigint not null,
    alert_status varchar null,
    alert_name varchar null,
    alert_text varchar null,
    alert_id varchar null
);

CREATE INDEX IF NOT EXISTS moira_alerts_timestamp_chatId on moira_alerts (timestamp, chat_id);

ALTER TABLE messages ALTER message_date TYPE timestamptz USING message_date AT TIME ZONE 'UTC';
ALTER TABLE messages ALTER edit_date TYPE timestamptz USING edit_date AT TIME ZONE 'UTC';
ALTER TABLE messages ALTER create_date TYPE timestamptz USING create_date AT TIME ZONE 'UTC';

ALTER TABLE moira_alerts ALTER message_date TYPE timestamptz USING message_date AT TIME ZONE 'UTC';

CREATE TABLE IF NOT EXISTS foods(
                                    id varchar PRIMARY KEY NOT NULL,
                                    name varchar NOT NULL,
                                    chat_id bigint NOT NULL,
                                    user_id bigint NOT NULL,
                                    kcal int NOT NULL,
                                    protein int NOT NULL,
                                    fat int NOT NULL,
                                    carbohydrate int NOT NULL,
                                    base_portion int NOT NULL
);

CREATE INDEX IF NOT EXISTS foods_name on foods (name);
CREATE INDEX IF NOT EXISTS foods_user_id on foods (user_id);

CREATE TABLE IF NOT EXISTS food_messages(
                                            id uuid PRIMARY KEY NOT NULL,
                                            food_id varchar NOT NULL,
                                            name varchar NOT NULL,
                                            chat_id bigint NOT NULL,
                                            user_id bigint NOT NULL,
                                            message_date timestamptz NOT NULL,
                                            create_date timestamptz NOT NULL,
                                            kcal int NOT NULL,
                                            protein int NOT NULL,
                                            fat int NOT NULL,
                                            carbohydrate int NOT NULL
);

CREATE INDEX IF NOT EXISTS food_messages_message_date on food_messages (message_date);
CREATE INDEX IF NOT EXISTS food_messages_user_id_message_date on food_messages (user_id, message_date);
CREATE INDEX IF NOT EXISTS food_messages_chat_id_message_date on food_messages (chat_id, message_date);

ALTER TABLE foods ADD COLUMN IF NOT EXISTS timestamp bigint default 0;

CREATE TABLE IF NOT EXISTS counters(
                                       id varchar PRIMARY KEY NOT NULL,
                                       user_name varchar NOT NULL,
                                       chat_id bigint NOT NULL,
                                       counter int NOT NULL,
                                       timestamp bigint NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_counters_user_name ON counters (user_name);
CREATE INDEX IF NOT EXISTS ix_counters_chat_id ON counters (chat_id);
CREATE INDEX IF NOT EXISTS ix_counters_timestamp ON counters (timestamp);

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
);

ALTER TABLE messages ADD COLUMN IF NOT EXISTS reactions_count int NOT NULL DEFAULT(0);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_messages_reactions_count_message_date ON messages (reactions_count, message_date);
ALTER TABLE messages ADD COLUMN IF NOT EXISTS reactions jsonb NOT NULL DEFAULT('[]');
CREATE INDEX CONCURRENTLY IF NOT exists ix_messages_chat_id_from_user_id_message_date ON messages (chat_id, from_user_id, message_date);
CREATE INDEX CONCURRENTLY IF NOT exists ix_messages_chat_id_from_user_id_reactions_count ON messages (chat_id, from_user_id, reactions_count);