using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MusicDatabase.Engine.Entities
{
    public class ReleaseDate
    {
        public ReleaseDateType Type { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public bool IsValid
        {
            get { return Type != ReleaseDateType.Invalid; }
        }

        /// <summary>
        /// Creates an invalid ReleaseDate
        /// </summary>
        public ReleaseDate()
        {
            this.Date = DateTime.MinValue;
            this.Type = ReleaseDateType.Invalid;
        }

        public ReleaseDate(int year)
        {
            this.Date = new DateTime(year, 1, 1);
            this.Type = ReleaseDateType.Year;
        }

        public ReleaseDate(int year, int month)
        {
            this.Date = new DateTime(year, month, 1);
            this.Type = ReleaseDateType.YearMonth;
        }

        public ReleaseDate(int year, int month, int day)
        {
            this.Date = new DateTime(year, month, day);
            this.Type = ReleaseDateType.YearMonthDay;
        }

        public override bool Equals(object obj)
        {
            ReleaseDate other = obj as ReleaseDate;
            if (other == null)
            {
                return false;
            }
            return other.Type == this.Type && other.Date == this.Date;
        }

        public override int GetHashCode()
        {
            return Utility.GetCombinedHashCode(this.Type, this.Date);
        }

        public override string ToString()
        {
            switch (this.Type)
            {
                case ReleaseDateType.Invalid:
                    return "";
                case ReleaseDateType.Year:
                    return this.Date.Year.ToString();
                case ReleaseDateType.YearMonth:
                    return this.Date.Year + "-" + this.Date.Month.ToString("00");
                case ReleaseDateType.YearMonthDay:
                    return this.Date.Year + "-" + this.Date.Month.ToString("00") + "-" + this.Date.Day.ToString("00");
                default:
                    throw new InvalidOperationException();
            }
        }

        public static ReleaseDate Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new ReleaseDate();
            }

            try
            {
                string[] parts = input.Split('-');
                int year = 0, month = 0, day = 0;

                if (parts.Length == 1)
                {
                    int.TryParse(parts[0], out year);
                }
                else if (parts.Length == 2)
                {
                    int.TryParse(parts[0], out year);
                    int.TryParse(parts[1], out month);
                }
                else if (parts.Length == 3)
                {
                    int.TryParse(parts[0], out year);
                    int.TryParse(parts[1], out month);
                    int.TryParse(parts[2], out day);
                }

                if (year == 0 && month == 0 && day == 0)
                {
                    return new ReleaseDate()
                    {
                        Type = ReleaseDateType.Invalid
                    };
                }
                if (month == 0 && day == 0)
                {
                    return new ReleaseDate()
                    {
                        Date = new DateTime(year, 1, 1),
                        Type = ReleaseDateType.Year
                    };
                }
                else if (day == 0)
                {
                    return new ReleaseDate()
                    {
                        Date = new DateTime(year, month, 1),
                        Type = ReleaseDateType.YearMonth
                    };
                }
                else
                {
                    return new ReleaseDate()
                    {
                        Date = new DateTime(year, month, day),
                        Type = ReleaseDateType.YearMonthDay
                    };
                }
            }
            catch
            {
            }

            return new ReleaseDate();
        }
    }
}
