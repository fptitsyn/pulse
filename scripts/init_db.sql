BEGIN;


CREATE TABLE IF NOT EXISTS public.check_results
(
    check_result_id serial NOT NULL,
    monitor_id integer NOT NULL,
    checked_at timestamp with time zone NOT NULL,
    http_code_id smallint,
    outcome character varying(30) COLLATE pg_catalog."default" NOT NULL,
    duration_ms integer NOT NULL,
    CONSTRAINT check_results_pkey PRIMARY KEY (check_result_id)
);

CREATE TABLE IF NOT EXISTS public.http_codes
(
    http_code_id smallserial NOT NULL,
    http_code smallint NOT NULL,
    CONSTRAINT http_codes_pkey PRIMARY KEY (http_code_id),
    CONSTRAINT http_codes_http_code_key UNIQUE (http_code)
);

CREATE TABLE IF NOT EXISTS public.http_methods
(
    http_method_id smallserial NOT NULL,
    http_method character varying COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT http_methods_pkey PRIMARY KEY (http_method_id),
    CONSTRAINT http_methods_http_method_key UNIQUE (http_method)
);

CREATE TABLE IF NOT EXISTS public.monitors
(
    monitor_id serial NOT NULL,
    service_id integer NOT NULL,
    path character varying COLLATE pg_catalog."default" NOT NULL,
    interval_seconds integer NOT NULL,
    http_method_id smallint NOT NULL,
    CONSTRAINT monitors_pkey PRIMARY KEY (monitor_id)
);

CREATE TABLE IF NOT EXISTS public.service_statuses
(
    service_status_id smallserial NOT NULL,
    service_status character varying COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT service_statuses_pkey PRIMARY KEY (service_status_id),
    CONSTRAINT service_statuses_service_status_key UNIQUE (service_status)
);

CREATE TABLE IF NOT EXISTS public.services
(
    service_id serial NOT NULL,
    user_id integer NOT NULL,
    base_url character varying COLLATE pg_catalog."default" NOT NULL,
    name character varying(30) COLLATE pg_catalog."default",
    service_status_id smallint NOT NULL,
    CONSTRAINT services_pkey PRIMARY KEY (service_id)
);

CREATE TABLE IF NOT EXISTS public.users
(
    user_id serial NOT NULL,
    email character varying(50) COLLATE pg_catalog."default" NOT NULL,
    password_hash character varying COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT users_pkey PRIMARY KEY (user_id)
);

ALTER TABLE IF EXISTS public.check_results
    ADD CONSTRAINT check_results_http_code_id_fkey FOREIGN KEY (http_code_id)
    REFERENCES public.http_codes (http_code_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION
    NOT VALID;


ALTER TABLE IF EXISTS public.check_results
    ADD CONSTRAINT check_results_monitor_id_fkey FOREIGN KEY (monitor_id)
    REFERENCES public.monitors (monitor_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION
    NOT VALID;


ALTER TABLE IF EXISTS public.monitors
    ADD CONSTRAINT monitors_http_method_id_fkey FOREIGN KEY (http_method_id)
    REFERENCES public.http_methods (http_method_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION
    NOT VALID;


ALTER TABLE IF EXISTS public.monitors
    ADD CONSTRAINT monitors_service_id_fkey FOREIGN KEY (service_id)
    REFERENCES public.services (service_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION
    NOT VALID;


ALTER TABLE IF EXISTS public.services
    ADD CONSTRAINT services_service_status_id_fkey FOREIGN KEY (service_status_id)
    REFERENCES public.service_statuses (service_status_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION
    NOT VALID;


ALTER TABLE IF EXISTS public.services
    ADD CONSTRAINT services_user_id_fkey FOREIGN KEY (user_id)
    REFERENCES public.users (user_id) MATCH SIMPLE
    ON UPDATE NO ACTION
    ON DELETE NO ACTION
    NOT VALID;

INSERT INTO service_statuses (service_status) VALUES ('UP'), ('DOWN'), ('UNKNOWN');
INSERT INTO http_methods (http_method) VALUES ('HEAD'), ('GET'), ('POST'), ('PUT'), ('PATCH'), ('DELETE');
INSERT INTO http_codes (http_code) VALUES (201), (204), (301), (302), (304), (400), (401), (403), (404), (500);

END;
