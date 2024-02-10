CREATE TABLE Tracks (
	Id INTEGER PRIMARY KEY,
	Name TEXT,
	Remote INTEGER,
	RemoteId TEXT,
	Artists TEXT,
	Album TEXT,
	Description TEXT,
	Rating INTEGER,
	TimeInSeconds INTEGER,
	RemoveMe BOOL Default FALSE -- remove when remote is gone
);