-- Database-per-service, run against the default admin connection (POSTGRES_DB
-- is intentionally left unset in docker-compose.yml so this can create each
-- database itself, same trick used by the (since-removed) Task 5 monolith's init.sql).
-- All five databases share one Postgres instance for this course project -
-- each service still only ever connects to its own database/connection
-- string, so ownership stays logically separated even though the container
-- footprint is one instance instead of five.
CREATE DATABASE userhome;
CREATE DATABASE devicemanagement;
CREATE DATABASE temperaturemonitoring;
CREATE DATABASE heatingcontrol;
CREATE DATABASE notification;
