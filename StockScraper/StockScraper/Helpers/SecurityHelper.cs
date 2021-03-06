﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Globalization;
using StockScraper.Models;
using StockScraper.Parsers;
using StockScraper.Utils;

namespace StockScraper.Helpers
{
    class SecurityHelper
    {
        private readonly StockScraperEntities _db = StockScraperEntitiesContext.Get();

        public Security CusipLookup(string cusip)
        {
            Security security = null;
            SecurityMap securityMap = _db.SecurityMaps.FirstOrDefault(s => s.Cusip == cusip);

            if (securityMap != null)
            {
                security = _db.Securities.FirstOrDefault(s => s.SecurityId == securityMap.SecurityId);
            }
            else
            {
                UnknownShare unknownShare = _db.UnknownShares.FirstOrDefault(s => s.Cusip == cusip);
                if (unknownShare == null) security = StockFinder.CusipLookup(cusip);
            }

            return security;
        }

        public Security NameLookup(string name)
        {
            name = NormaliseSecurityName(name);

            SecurityMap securityMap = _db.SecurityMaps.FirstOrDefault(s => s.Name == name);
            Security security = null;

            if (securityMap != null)
            {
                security = _db.Securities.FirstOrDefault(s => s.SecurityId == securityMap.SecurityId);
            }
            else
            {
                UnknownShare unknownShare = _db.UnknownShares.FirstOrDefault(s => s.NameOfIssuer == name);
                if (unknownShare == null) security = StockFinder.NameLookup(name);
            }

            return security;
        }

        public SecurityMap SaveSecurityMap(Security security, string cusip)
        {
            SecurityMap securityMap;

            try
            {
                securityMap = new SecurityMap
                {
                    Cusip = cusip,
                    Name = security.Name,
                    SecurityId = security.SecurityId
                };

                _db.SecurityMaps.Add(securityMap);

                _db.SaveChanges();
            }
            catch
            {
                securityMap = _db.SecurityMaps.FirstOrDefault(s => s.Cusip == cusip);
                if (securityMap == null) throw;
            }

            return securityMap;
        }

        public Security SaveSecurity(Security security)
        {
            if (security == null) return null;

            try
            {
                _db.Securities.Add(security);
                _db.SaveChanges();
            }
            catch
            {
                _db.Entry(security).State = EntityState.Detached;
                security = _db.Securities.FirstOrDefault(s => s.Symbol == security.Symbol && s.Exchange == security.Exchange);
                if (security == null) throw;
            }

            return security;
        }

        private string NormaliseSecurityName(string name)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            name = textInfo.ToLower(name.TrimEnd());
            name = textInfo.ToTitleCase(name);

            StringBuilder sb = new StringBuilder(name.Length);
            char lastChar = '\0';
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '&' || char.IsWhiteSpace(c) & !char.IsWhiteSpace(lastChar))
                {
                    sb.Append(lastChar = c);
                }
            }

            return sb.ToString();
        }

        public void AddUnknownShare(InfoTableItem infoTableItem, int filingId)
        {
            Console.WriteLine($"Unable to find security for {infoTableItem.NameOfIssuer}");

            var unknownShare = new UnknownShare
            {
                FilingId = filingId,
                Number = infoTableItem.NumberOfShares,
                Type = infoTableItem.TypeOfShares,
                Value = infoTableItem.Value,
                NameOfIssuer = infoTableItem.NameOfIssuer,
                Cusip = infoTableItem.Cusip,
            };

            _db.UnknownShares.Add(unknownShare);
            _db.SaveChanges();
        }
    }
}
