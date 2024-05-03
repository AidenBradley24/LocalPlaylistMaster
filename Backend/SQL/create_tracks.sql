CREATE TABLE Tracks (
	Id INTEGER PRIMARY KEY,
	Name TEXT,
	Remote INTEGER,
	RemoteId TEXT,
	Artists TEXT,
	Album TEXT,
	Description TEXT,
	Rating INTEGER,
	TimeInSeconds REAL,
	Settings INTEGER,
	MiscJson Text
);