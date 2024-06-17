CREATE TABLE SignAppDB.users ( id  INT primary KEY not null AUTO_INCREMENT, username VARCHAR(50) unique, password VARCHAR(64),level int);
CREATE TABLE SignAppDB.session (sessionKey varchar(64) PRIMARY KEY, userid int, keyExpiration datetime);
CREATE TABLE SignAppDB.files ( id  INT primary KEY not null AUTO_INCREMENT, filename varchar(200), hash varchar(64), owner int, creationtime datetime);
CREATE TABLE SignAppDB.perms (id  INT primary KEY not null AUTO_INCREMENT, user int, readperm bool, writeperm bool, delperm bool, fileid int);
CREATE TABLE SignAppDB.userKeys (id  INT primary KEY not null AUTO_INCREMENT, userid int, name varchar(100), pubkey varchar(2000), prikey varchar(2000),hash varchar(64) unique);
CREATE TABLE SignAppDB.signatures (id  INT primary KEY not null AUTO_INCREMENT, userid int, keyid int, signature varchar(344),fileid int, creationtime datetime); 