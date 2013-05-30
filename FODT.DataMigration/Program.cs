﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using FODT.Database;
using FODT.Models.Entities;

namespace FODT.DataMigration
{
    public class Program
    {
        public const string DocumentStoreUrl = @"http://localhost:8080";
        public const string DataDumpDirectory = @"C:\src\FriendsOfDT\data\";

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var connectionString = ConfigurationManager.ConnectionStrings["fodt"].ConnectionString;
            var cfg = DatabaseBootstrapper.Bootstrap(connectionString);
            var sessionFactory = cfg.BuildSessionFactory();

            ImportAwardsList(sessionFactory);
            ImportPersons(sessionFactory);
            ImportShows(sessionFactory);
            ImportShowAwards(sessionFactory);
            ImportPersonAwards(sessionFactory);
            ImportCast(sessionFactory);
            ImportCrew(sessionFactory);
            ImportEC(sessionFactory);
        }

        private static void ImportShows(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var shows = LoadEntities("shows_fixed.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].Show ON").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in shows)
                {
                    if (string.IsNullOrWhiteSpace(_row[1]))
                    {
                        // no title? no show
                        continue;
                    }
                    var entity = new Show();
                    entity.ShowId = int.Parse(_row[0]);
                    entity.Title = (_row[1] ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(_row[2]))
                    {
                        entity.Quarter = byte.Parse(_row[2]);
                    }
                    else
                    {
                        // TODO?
                    }
                    entity.Author = (_row[3] ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(_row[4]))
                    {
                        entity.Year = short.Parse(_row[4]);
                    }
                    else
                    {
                        // TODO?
                    }
                    entity.Pictures = (_row[5] ?? string.Empty).Trim();
                    entity.FunFacts = (_row[6] ?? string.Empty).Trim();
                    entity.Toaster = (_row[9] ?? string.Empty).Trim();
                    entity.MediaId = int.Parse(_row[8]);
                    entity.InsertedDateTime = DateTime.UtcNow;
                    entity.LastModifiedDateTime = DateTime.UtcNow;
                    var lastModifiedDateTime = DateTime.UtcNow;
                    if (DateTime.TryParse(_row[7], out lastModifiedDateTime))
                    {
                        entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                    }
                    session.Save(entity, entity.ShowId);
                    if (entity.ShowId > maxId) maxId = entity.ShowId;
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].Show OFF").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.Show', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportPersons(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var people = LoadEntities("people_fixed.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].Person ON").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in people)
                {
                    var entity = new Person();
                    entity.PersonId = int.Parse(_row[0]);
                    entity.Honorific = (_row[1] ?? string.Empty).Trim();
                    entity.FirstName = (_row[2] ?? string.Empty).Trim();
                    entity.MiddleName = (_row[3] ?? string.Empty).Trim();
                    entity.LastName = (_row[4] ?? string.Empty).Trim();
                    entity.Suffix = (_row[5] ?? string.Empty).Trim();
                    entity.Nickname = (_row[6] ?? string.Empty).Trim();
                    entity.Biography = (_row[7] ?? string.Empty).Trim();
                    entity.MediaId = int.Parse(_row[13]);
                    entity.InsertedDateTime = DateTime.UtcNow;
                    entity.LastModifiedDateTime = DateTime.UtcNow;
                    var lastModifiedDateTime = DateTime.UtcNow;
                    if (DateTime.TryParse(_row[12], out lastModifiedDateTime))
                    {
                        entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                    }
                    session.Save(entity, entity.PersonId);
                    if (entity.PersonId > maxId) maxId = entity.PersonId;
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].Person OFF").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.Person', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportAwardsList(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var awards = LoadEntities("awards_list.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].Award ON").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in awards)
                {
                    var entity = new Award();
                    entity.AwardId = int.Parse(_row[0]);
                    entity.Name = (_row[1] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(entity.Name))
                    {
                        entity.Name = "[MISSING]";
                    }
                    session.Save(entity, entity.AwardId);
                    if (entity.AwardId > maxId) maxId = entity.AwardId;
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].Award OFF").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.Award', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportShowAwards(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var awards = LoadEntities("awards.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].ShowAward ON;").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in awards)
                {
                    int showId = 0;
                    if (int.TryParse(_row[1], out showId))
                    {
                        var entity = new ShowAward();
                        entity.ShowAwardId = int.Parse(_row[0]);
                        entity.Show = session.Load<Show>(showId);
                        int personId = 0;
                        if (int.TryParse(_row[2], out personId))
                        {
                            entity.Person = session.Load<Person>(personId);
                        }
                        entity.Award = session.Load<Award>(int.Parse(_row[3]));
                        entity.Year = short.Parse(_row[4]);
                        entity.InsertedDateTime = DateTime.UtcNow;
                        entity.LastModifiedDateTime = DateTime.UtcNow;
                        var lastModifiedDateTime = DateTime.UtcNow;
                        if (DateTime.TryParse(_row[5], out lastModifiedDateTime))
                        {
                            entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                        }
                        session.Save(entity, entity.ShowAwardId);
                        if (entity.ShowAwardId > maxId) maxId = entity.ShowAwardId;
                    }
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].ShowAward OFF;").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.ShowAward', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportPersonAwards(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var awards = LoadEntities("awards.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].PersonAward ON;").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in awards)
                {
                    int showId = 0;
                    int personId = 0;
                    if (!int.TryParse(_row[1], out showId) && int.TryParse(_row[2], out personId))
                    {
                        // no show, only a person
                        var entity = new PersonAward();
                        entity.PersonAwardId = int.Parse(_row[0]);
                        entity.Person = session.Load<Person>(personId);
                        entity.Award = session.Load<Award>(int.Parse(_row[3]));
                        entity.Year = short.Parse(_row[4]);
                        entity.InsertedDateTime = DateTime.UtcNow;
                        entity.LastModifiedDateTime = DateTime.UtcNow;
                        var lastModifiedDateTime = DateTime.UtcNow;
                        if (DateTime.TryParse(_row[5], out lastModifiedDateTime))
                        {
                            entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                        }
                        session.Save(entity, entity.PersonAwardId);
                        if (entity.PersonAwardId > maxId) maxId = entity.PersonAwardId;
                    }
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].PersonAward OFF;").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.PersonAward', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportCast(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var cast = LoadEntities("cast.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].ShowCast ON;").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in cast)
                {
                    if (string.IsNullOrWhiteSpace(_row[1]) || string.IsNullOrWhiteSpace(_row[2]))
                    {
                        // broken?
                        continue;
                    }
                    var entity = new ShowCast();
                    entity.ShowCastId = int.Parse(_row[0]);
                    entity.Person = session.Load<Person>(int.Parse(_row[1]));
                    entity.Show = session.Load<Show>(int.Parse(_row[2]));
                    entity.Role = (_row[3] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(entity.Role))
                    {
                        entity.Role = "[MISSING]";
                    }
                    entity.InsertedDateTime = DateTime.UtcNow;
                    entity.LastModifiedDateTime = DateTime.UtcNow;
                    var lastModifiedDateTime = DateTime.UtcNow;
                    if (DateTime.TryParse(_row[4], out lastModifiedDateTime))
                    {
                        entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                    }
                    session.Save(entity, entity.ShowCastId);
                    if (entity.ShowCastId > maxId) maxId = entity.ShowCastId;
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].ShowCast OFF;").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.ShowCast', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportCrew(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var jobs = LoadEntities("jobs.txt").Select(x =>
                {
                    int id = 0;
                    if (!int.TryParse(x[0], out id))
                    {
                        return null;
                    }
                    int order = 0;
                    if (!int.TryParse(x[2], out order)) order = 999;
                    return new
                    {
                        id = id,
                        name = x[1],
                        order = order,
                    };
                }).Where(x => x != null).ToDictionary(x => x.id);
                var crew = LoadEntities("crew.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].ShowCrew ON;").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in crew)
                {
                    if (string.IsNullOrWhiteSpace(_row[1]) || string.IsNullOrWhiteSpace(_row[2]))
                    {
                        // broken?
                        continue;
                    }
                    var entity = new ShowCrew();
                    entity.ShowCrewId = int.Parse(_row[0]);
                    entity.Person = session.Load<Person>(int.Parse(_row[1]));
                    entity.Show = session.Load<Show>(int.Parse(_row[2]));
                    int jobId = int.Parse(_row[3]);
                    entity.DisplayOrder = jobs[jobId].order;
                    entity.Position = jobs[jobId].name;
                    if (string.IsNullOrWhiteSpace(entity.Position))
                    {
                        entity.Position = "[MISSING]";
                    }
                    entity.InsertedDateTime = DateTime.UtcNow;
                    entity.LastModifiedDateTime = DateTime.UtcNow;
                    var lastModifiedDateTime = DateTime.UtcNow;
                    if (DateTime.TryParse(_row[4], out lastModifiedDateTime))
                    {
                        entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                    }
                    session.Save(entity, entity.ShowCrewId);
                    if (entity.ShowCrewId > maxId) maxId = entity.ShowCrewId;
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].ShowCrew OFF;").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.ShowCrew', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        private static void ImportEC(NHibernate.ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var ec_list = LoadEntities("ec_list.txt").Select(x =>
                {
                    int id = 0;
                    if (!int.TryParse(x[0], out id))
                    {
                        return null;
                    }
                    return new
                    {
                        id = id,
                        name = x[1],
                    };
                }).Where(x => x != null).ToDictionary(x => x.id);
                var ec = LoadEntities("ec.txt");
                session.Transaction.Begin();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].PersonClubPosition ON;").ExecuteUpdate();
                var maxId = 0;
                foreach (var _row in ec)
                {
                    if (string.IsNullOrWhiteSpace(_row[1]))
                    {
                        // broken?
                        continue;
                    }
                    var entity = new PersonClubPosition();
                    entity.PersonClubPositionId = int.Parse(_row[0]);
                    entity.Person = session.Load<Person>(int.Parse(_row[1]));
                    int ecId = int.Parse(_row[2]);
                    entity.Position = ec_list[ecId].name;
                    entity.Year = short.Parse(_row[3]);
                    if (string.IsNullOrWhiteSpace(entity.Position))
                    {
                        entity.Position = "[MISSING]";
                    }
                    entity.DisplayOrder = ecId;
                    entity.InsertedDateTime = DateTime.UtcNow;
                    entity.LastModifiedDateTime = DateTime.UtcNow;
                    var lastModifiedDateTime = DateTime.UtcNow;
                    if (DateTime.TryParse(_row[4], out lastModifiedDateTime))
                    {
                        entity.LastModifiedDateTime = TimeZoneInfo.ConvertTimeToUtc(lastModifiedDateTime, TimeZoneCode.Eastern.ToTimeZoneInfo());
                    }
                    session.Save(entity, entity.PersonClubPositionId);
                    if (entity.PersonClubPositionId > maxId) maxId = entity.PersonClubPositionId;
                }
                session.Flush();
                session.CreateSQLQuery("SET IDENTITY_INSERT [imdt].PersonClubPosition OFF;").ExecuteUpdate();
                session.CreateSQLQuery("DBCC CHECKIDENT ('imdt.PersonClubPosition', RESEED, " + (maxId + 1) + ")").ExecuteUpdate();
                session.Transaction.Commit();
                session.Close();
            }
        }

        public static List<IDelimitedRow> LoadEntities(string file)
        {
            return new TSVReader(File.OpenRead(Path.Combine(DataDumpDirectory, file)), false).ToList();
        }
    }
}
