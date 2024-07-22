# TimeTrackerAPI


Script to create the Database Table:

-- Table: timetracker.Users

-- DROP TABLE IF EXISTS timetracker."Users";

CREATE TABLE IF NOT EXISTS timetracker."Users"
(
    "Id" bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ),
    
    "Login" character varying COLLATE pg_catalog."default" NOT NULL,
    
    "Email" character varying COLLATE pg_catalog."default",
    
    "PasswordHash" character varying COLLATE pg_catalog."default" NOT NULL,
    
    CONSTRAINT "User_pkey" PRIMARY KEY ("Id")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS timetracker."Users"
    OWNER to postgres;
