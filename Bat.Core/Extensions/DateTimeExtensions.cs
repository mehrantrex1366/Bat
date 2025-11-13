namespace Bat.Core;

public static class DateTimeExtensions
{
    extension(DateTime date)
    {
        public PersianDateTime ToPersianDateTime() => PersianDateTime.Parse(date);

        public string ToPersianDate() => PersianDateTime.Parse(date).ToString();

        public string ToTime() => date.ToString("HH:mm");

        public string ToFullTime() => date.ToString("HH:mm:ss");

        public DateOnly ToDateOnly() => DateOnly.FromDateTime(date);

        public TimeOnly ToTimeOnly() => TimeOnly.FromDateTime(date);


        public bool IsFuture() => date.IsFuture(DateTime.Now);

        public bool IsFuture(DateTime from) => date.Date > from.Date;

        public bool IsPast() => date.IsPast(DateTime.Now);

        public bool IsPast(DateTime from) => date.Date < from.Date;
    }

    extension(DateTime? date)
    {
        public PersianDateTime ToPersianDateTime() => (date.IsNull() || date == DateTime.MinValue) ? null : PersianDateTime.Parse((DateTime)date);

        public string ToPersianDate() => (date.IsNull() || date == DateTime.MinValue) ? string.Empty : PersianDateTime.Parse(((DateTime)date)).ToString();

        public string ToTime() => (date.IsNull() || date == DateTime.MinValue) ? string.Empty : ((DateTime)date).ToString("HH:mm");

        public string ToFullTime() => (date.IsNull() || date == DateTime.MinValue) ? string.Empty : ((DateTime)date).ToString("HH:mm:ss");

        public DateOnly ToDateOnly() => (date is null || date == DateTime.MinValue) ? DateOnly.MinValue : DateOnly.FromDateTime((DateTime)date);

        public TimeOnly ToTimeOnly() => (date is null || date == DateTime.MinValue) ? TimeOnly.MinValue : TimeOnly.FromDateTime((DateTime)date);

    }

    extension(string persianDateTime)
    {
        public DateTime ToDateTime() => PersianDateTime.Parse(persianDateTime).ToDateTime();

        public DateTime ToDateTime(int hour, int minute) => PersianDateTime.Parse(persianDateTime).ToDateTime().AddHours(hour).AddMinutes(minute);

        public DateTime ToDateTime(int hour, int minute, int second) => PersianDateTime.Parse(persianDateTime).ToDateTime().AddHours(hour).AddMinutes(minute).AddSeconds(second);
    }
}