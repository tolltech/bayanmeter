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