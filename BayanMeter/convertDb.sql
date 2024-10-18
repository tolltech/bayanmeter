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