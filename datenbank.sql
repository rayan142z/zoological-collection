create table users(
  id integer auto_increment primary key ,
  username varchar(100) unique not null,
  email varchar(150) unique not null,
  pass varchar(255) not null,
  user_role ENUM('admin','user','moderator') not null,
  created_at timestamp default current_timestamp
);

create table taxonomy(
  id integer auto_increment primary key,
  kingdom varchar(100),
  phylum varchar(100),
  class varchar(100),
  orders varchar(100),
  family varchar(100),
  genus varchar(100),
  species varchar(100) not null
);

create table location (
  id int auto_increment primary key,
  name varchar(150),
  latitude decimal(9,6),
  longitude decimal(9,6),
  country varchar(100),
  region varchar(100)
);

create table collection(
  id int auto_increment primary key,
  name varchar(150) not null,
  description text,
  created_by int not null,
  constraint fk_creator
  foreign key (created_by)
  references users(id),
  created_at timestamp default current_timestamp
);

create table specimen(
  id integer auto_increment primary key ,
  name varchar(150) not null,
  description text ,
  date_collected date,
  status ENUM('available', 'lost', 'on loan', 'destroyrd' ),
  location_id integer not null,
  constraint fk_location
  foreign key (location_id)
  references location(id),
  taxonomy_id integer not null, 
  constraint fk_taxonomy
  foreign key (taxonomy_id)
  references taxonomy(id),
  collection_id integer not null, 
  constraint fk_collection
  foreign key (collection_id)
  references collection(id),
  added_by int not null,
  constraint fk_added
  foreign key (added_by)
  references users(id),
  created_at timestamp default current_timestamp
);

create table loan (
  id int auto_increment primary key,
  specimen_id int ,
  constraint fk_specimanloan
  foreign key (specimen_id)
  references specimen(id),
  loaned_to varchar(150) not null,
  loan_date date not null,
  return_date date,
  status enum('active', 'returned', 'overdue') ,
  notes text
);



 
