using System;
using System.Collections.Generic;
using System.Text;
using iTunesLib;
using System.Data.SQLite;

namespace JamesRSkemp.iTunes.MusicToSQLite {
	class Program {

		internal string databaseFileName = "iTunesMusic.s3db";
		internal string singleSourceName = "";

		internal iTunesApp iTunes = null;
		internal IITSourceCollection sources = null;

		static void Main(string[] args) {

			Program program = new Program();

			if (args.Length == 2) {
				if (args[0] != "null") {
					program.databaseFileName = args[0];
				}
				program.singleSourceName = args[1];
			}

			bool ContinueWorking = true;

			if (ContinueWorking) {
				ContinueWorking = program.VerifyDatabaseFile();
			}

			if (ContinueWorking) {
				ContinueWorking = program.ConnectToiTunes();
			}

			if (ContinueWorking) {
				ContinueWorking = program.GetSourceListing();
			}

			if (ContinueWorking) {
				ContinueWorking = program.SetupTracksTable();
			}

			if (ContinueWorking) {
				foreach (IITSource source in program.sources) {
					if (program.singleSourceName == "" || source.Name == program.singleSourceName) {
						program.GetTrackListing(source);
					}
				}
			}

			Console.WriteLine("Processing finished. Press any key to quit.");
			Console.Read();
		}

		/// <summary>
		/// Verifies that there is a SQLite database in the installation directory.
		/// </summary>
		/// <returns>Boolean on whether a SQLite database exists.</returns>
		private bool VerifyDatabaseFile() {
			try {
				SQLiteConnection connection = new SQLiteConnection();

				connection.ConnectionString = "Data Source=" + databaseFileName;
				connection.Open();

				SQLiteCommand command = new SQLiteCommand();
				command.Connection = connection;
				command.CommandType = System.Data.CommandType.Text;

				command.CommandText = "CREATE TABLE IF NOT EXISTS TestTable (id INTEGER PRIMARY KEY, string TEXT)";
				command.ExecuteNonQuery();

				command.CommandText = "DROP TABLE IF EXISTS TestTable";
				command.ExecuteNonQuery();

				command.Dispose();

				connection.Close();

				connection.Dispose();

				//Console.WriteLine("Test table created and dropped.");
				return true;

			} catch (Exception ex) {
				Console.WriteLine("Unable to create and drop a test table.");
				Console.WriteLine("Error message: " + ex.Message);
				return false;
			}
		}

		private bool ConnectToiTunes() {
			try {
				iTunes = new iTunesAppClass();

				return true;
			} catch (Exception ex) {
				Console.WriteLine("Unable to connect to iTunes.");
				Console.WriteLine("Error message: " + ex.Message);
				return false;
			}
		}

		private bool GetSourceListing() {
			try {
				if (iTunes.Sources.Count > 0) {
					sources = iTunes.Sources;
					return true;
				} else {
					Console.WriteLine("There are no sources available.");
					return false;
				}
			} catch (Exception ex) {
				Console.WriteLine("Unable to get a listing of sources from iTunes.");
				Console.WriteLine("Error message: " + ex.Message);
				return false;
			}
		}

		private bool SetupTracksTable() {
			try {

				using (SQLiteConnection connection = new SQLiteConnection()) {
					connection.ConnectionString = "Data Source=" + databaseFileName;
					connection.Open();

					SQLiteCommand command = new SQLiteCommand();
					command.Connection = connection;
					command.CommandType = System.Data.CommandType.Text;

					command.CommandText = "DROP TABLE IF EXISTS iTunesLibrary";
					command.ExecuteNonQuery();

					command.CommandText = "CREATE TABLE IF NOT EXISTS iTunesLibrary (id INTEGER PRIMARY KEY, Name TEXT, Album TEXT, Artist TEXT, Compilation TEXT, DateAdded TEXT, DiscCount INTEGER, DiscNumber INTEGER, Genre TEXT, Kind TEXT, PlayedCount INTEGER, PlayedDate TEXT, Rating INTEGER, Time TEXT, TrackCount INTEGER, TrackNumber INTEGER, Year INTEGER, Source TEXT)";
					command.ExecuteNonQuery();

					command.Dispose();

					connection.Close();
					connection.Dispose();
				}
				return true;
			} catch (Exception ex) {
				Console.WriteLine("Unable to create tracks table.");
				Console.WriteLine("Error message: " + ex.Message);
				return false;
			}
		}

		private bool GetTrackListing(IITSource source) {
			try {
				if (source.Kind == ITSourceKind.ITSourceKindLibrary || source.Kind == ITSourceKind.ITSourceKindIPod) {
					if (source.Playlists.Count > 0) {
						foreach (IITPlaylist playlist in source.Playlists) {
							if (playlist.Name == "Music" && playlist.Tracks.Count > 0) {
								WriteTracks(playlist.Tracks, source.Name);
								Console.WriteLine(source.Name + " tracks written.");
							}
						}
					}
				}
				return true;
			} catch (Exception ex) {
				Console.WriteLine("Unable to get a track listing for source " + source.Name + ".");
				Console.WriteLine("Error message: " + ex.Message);
				return false;
			}
		}

		private bool WriteTracks(IITTrackCollection tracks, string sourceName) {
			try {
				SQLiteConnection connection = new SQLiteConnection();

				connection.ConnectionString = "Data Source=" + databaseFileName;
				connection.Open();

				SQLiteCommand command = new SQLiteCommand();
				command.Connection = connection;
				command.CommandType = System.Data.CommandType.Text;

				using (SQLiteTransaction transaction = connection.BeginTransaction()) {

					command.CommandText = "INSERT INTO iTunesLibrary (Name, Album, Artist, Compilation, DateAdded, DiscCount, DiscNumber, Genre, Kind, PlayedCount, PlayedDate, Rating, Time, TrackCount, TrackNumber, Year, Source) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
					command.Parameters.Add(new SQLiteParameter("paramName"));
					command.Parameters.Add(new SQLiteParameter("paramAlbum"));
					command.Parameters.Add(new SQLiteParameter("paramArtist"));
					command.Parameters.Add(new SQLiteParameter("paramCompilation"));
					command.Parameters.Add(new SQLiteParameter("paramDateAdded"));
					command.Parameters.Add(new SQLiteParameter("paramDiscCount"));
					command.Parameters.Add(new SQLiteParameter("paramDiscNumber"));
					command.Parameters.Add(new SQLiteParameter("paramGenre"));
					command.Parameters.Add(new SQLiteParameter("paramKind"));
					command.Parameters.Add(new SQLiteParameter("paramPlayedCount"));
					command.Parameters.Add(new SQLiteParameter("paramPlayedDate"));
					command.Parameters.Add(new SQLiteParameter("paramRating"));
					command.Parameters.Add(new SQLiteParameter("paramTime"));
					command.Parameters.Add(new SQLiteParameter("paramTrackCount"));
					command.Parameters.Add(new SQLiteParameter("paramTrackNumber"));
					command.Parameters.Add(new SQLiteParameter("paramYear"));
					command.Parameters.Add(new SQLiteParameter("paramSource"));
					command.Parameters["paramSource"].Value = sourceName;

					foreach (IITTrack track in tracks) {
						if (track.KindAsString != "QuickTime movie file" && track.KindAsString != "PDF document") {
							command.Parameters["paramName"].Value = track.Name;
							command.Parameters["paramAlbum"].Value = track.Album;
							command.Parameters["paramArtist"].Value = track.Artist;
							command.Parameters["paramCompilation"].Value = track.Compilation;
							command.Parameters["paramDateAdded"].Value = track.DateAdded;
							command.Parameters["paramDiscCount"].Value = track.DiscCount;
							command.Parameters["paramDiscNumber"].Value = track.DiscNumber;
							command.Parameters["paramGenre"].Value = track.Genre;
							command.Parameters["paramKind"].Value = track.KindAsString;
							command.Parameters["paramPlayedCount"].Value = track.PlayedCount;
							command.Parameters["paramPlayedDate"].Value = track.PlayedDate;
							command.Parameters["paramRating"].Value = track.Rating;
							command.Parameters["paramTime"].Value = track.Time;
							command.Parameters["paramTrackCount"].Value = track.TrackCount;
							command.Parameters["paramTrackNumber"].Value = track.TrackNumber;
							command.Parameters["paramYear"].Value = track.Year;
							command.ExecuteNonQuery();
						}
					}
					transaction.Commit();
				}
				
				command.Dispose();

				connection.Close();
				connection.Dispose();
				return true;
			} catch (Exception ex) {
				Console.WriteLine("Unable to write tracks.");
				Console.WriteLine("Error message: " + ex.Message);
				return false;
			}
		}
	}
}
