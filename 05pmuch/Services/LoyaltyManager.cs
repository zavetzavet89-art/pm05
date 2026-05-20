using System;
using System.Linq;
using _05pmuch.Data;
using _05pmuch.Models;

namespace _05pmuch.Services
{
    public static class LoyaltyManager
    {
        public const int PointsToSpend = 100;
        public const decimal RubDiscountPerSpend = 1000m;

        public static decimal CalculateDiscountPercent(decimal totalPaid)
        {
            if (totalPaid >= 150000m) return 10m;
            if (totalPaid >= 50000m) return 5m;
            return 0m;
        }

        public static decimal CalculateFinalAmount(decimal baseCost, decimal currentDiscount, bool spendPoints, int loyaltyPoints)
        {
            var amount = baseCost * (1m - currentDiscount / 100m);
            if (spendPoints && loyaltyPoints >= PointsToSpend)
                amount -= RubDiscountPerSpend;
            return Math.Max(0m, amount);
        }

        public static int CalculatePointsEarned(decimal paymentAmount)
        {
            return (int)Math.Floor(paymentAmount / 10000m) * 100;
        }

        public static void ApplyPaymentEffects(int bookingId)
        {
            using (var db = new ApplicationDbContext())
            {
                ApplyPaymentEffects(bookingId, db);
            }
        }

        public static void ApplyPaymentEffects(int bookingId, ApplicationDbContext db)
        {
            var booking = db.Bookings.Find(bookingId);
            if (booking == null)
                return;

            var paidTotal = db.Payments
                .Where(p => p.BookingId == bookingId)
                .Sum(p => (decimal?)p.SumPaid) ?? 0m;

            if (booking.BookingStatus != BookingStatuses.Cancelled)
            {
                if (paidTotal >= booking.FinalAmount && booking.FinalAmount > 0)
                    booking.BookingStatus = BookingStatuses.Paid;
                else if (booking.BookingStatus == BookingStatuses.Paid)
                    booking.BookingStatus = BookingStatuses.AwaitingPayment;
            }

            var clientId = booking.ClientId;
            RecalculateClientLoyalty(clientId, db);
            db.SaveChanges();
        }

        public static void RecalculateClientLoyalty(int clientId, ApplicationDbContext db)
        {
            var client = db.Clients.Find(clientId);
            if (client == null)
                return;

            var totalPaid = (
                from p in db.Payments
                join b in db.Bookings on p.BookingId equals b.Id
                where b.ClientId == clientId
                select p.SumPaid).DefaultIfEmpty(0m).Sum();

            client.CurrentDiscount = CalculateDiscountPercent(totalPaid);

            var earnedPoints = (
                from p in db.Payments
                join b in db.Bookings on p.BookingId equals b.Id
                where b.ClientId == clientId
                select CalculatePointsEarned(p.SumPaid)).DefaultIfEmpty(0).Sum();

            client.LoyaltyPoints = Math.Max(client.LoyaltyPoints, earnedPoints);
        }
    }
}
