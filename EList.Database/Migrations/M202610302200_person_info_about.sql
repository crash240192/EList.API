-- Публичный текст «о себе» в профиле пользователя (без шифрования ПДн).
ALTER TABLE public.person_info
  ADD COLUMN IF NOT EXISTS about text NULL;
