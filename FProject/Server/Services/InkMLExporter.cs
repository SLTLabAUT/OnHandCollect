using FProject.Server.Data;
using FProject.Server.Models;
using FProject.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FProject.Server.Services
{
    public class InkMLExporter
    {
        public static XDocument WriterTemplate;
        public static XDocument GTTemplate;
        public static XDocument WritepadTemplate;

        private readonly ApplicationDbContext _context;

        public InkMLExporter(ApplicationDbContext context)
        {
            _context = context;
        }

        public static async Task Initialize()
        {
            var settings = new XmlReaderSettings { Async = true, IgnoreWhitespace = true };

            var reader = XmlReader.Create("Data/InkMLs/Templates/Writepads.xml", settings);
            WritepadTemplate = await XDocument.LoadAsync(reader, default, default);
            reader.Close();
            reader.Dispose();

            reader = XmlReader.Create("Data/InkMLs/Templates/Writers.xml", settings);
            WriterTemplate = await XDocument.LoadAsync(reader, default, default);
            reader.Close();
            reader.Dispose();

            reader = XmlReader.Create("Data/InkMLs/Templates/GroundTruths.xml", settings);
            GTTemplate = await XDocument.LoadAsync(reader, default, default);
            GTTemplate.Root.RemoveNodes();
            reader.Close();
            reader.Dispose();
        }

        public async Task<int> ExportWritepads(DateTimeOffset start, DateTimeOffset end)
        {
            IQueryable<Writepad> query = _context.Writepads.IgnoreQueryFilters()
                .Where(w => w.Status == WritepadStatus.Accepted && w.LastModified >= start && w.LastModified < end);
            var count = await query.CountAsync().ConfigureAwait(false);
            if (count == 0)
            {
                return count;
            }
            var partsRange = 200;
            var partsCount = Math.Ceiling((double)count / partsRange);

            var folderName = $"{start.ToUniversalTime().ToString("yy-MM-dd HH-mm")} to {end.ToUniversalTime().ToString("yy-MM-dd HH-mm")} ({DateTime.UtcNow.ToString("yy-MM-dd HH-mm-ss")})";
            Directory.CreateDirectory($"Data/InkMLs/Dataset/Writepads/{folderName}");
            foreach (var type in Enum.GetValues<WritepadType>())
            {
                Directory.CreateDirectory($"Data/InkMLs/Dataset/Writepads/{folderName}/{type}");
            }

            for (int p = 0; p < partsCount; p++)
            {
                var writepads = await query
                    .OrderBy(w => w.Id)
                    .Skip(p * partsRange)
                    .Take(partsRange)
                    .ToListAsync().ConfigureAwait(false);

                XNamespace ns = null;
                foreach (var writepad in writepads)
                {
                    await Task.Delay(100).ConfigureAwait(false);

                    await _context.Entry(writepad)
                        .Collection(w => w.Points)
                        .Query()
                        .OrderBy(point => point.Number)
                        .LoadAsync().ConfigureAwait(false);

                    var document = new XDocument(WritepadTemplate);
                    if (ns is null)
                    {
                        ns = document.Root.GetDefaultNamespace();
                    }

                    document.Root.SetAttributeValue("documentID", writepad.Id);

                    var annotation = document.Descendants(ns + "annotationXML").First();
                    annotation.SetElementValue("writerId", $"urn:uuid:{writepad.OwnerId}");
                    annotation.SetElementValue("input", writepad.PointerType);
                    annotation.SetElementValue("type", writepad.Type);
                    annotation.SetElementValue("hand", writepad.Hand);
                    annotation.SetElementValue("truthId", writepad.TextId);

                    double timeOrigin = 0;
                    var strokePoints = new List<DrawingPoint>();
                    var strokeStarted = false;
                    var strokeEnded = false;
                    var traceGroup = document.Descendants(ns + "traceGroup").First();
                    foreach (var point in writepad.Points)
                    {
                        var strokeCompleted = false;
                        switch (point.Type)
                        {
                            case PointType.Starting:
                                strokeStarted = true;
                                strokeEnded = false;

                                strokePoints.Clear();
                                strokePoints.Add(point);

                                if (timeOrigin == 0)
                                {
                                    timeOrigin = point.TimeStamp;
                                }
                                break;
                            case PointType.Middle:
                                if (!strokeStarted || strokeEnded)
                                {
                                    continue;
                                }

                                strokePoints.Add(point);
                                break;
                            case PointType.Ending:
                                if (!strokeStarted || strokeEnded)
                                {
                                    continue;
                                }
                                strokeStarted = false;
                                strokeEnded = true;

                                strokePoints.Add(point);

                                strokeCompleted = true;
                                break;
                        }

                        if (!strokeCompleted)
                        {
                            continue;
                        }

                        var stroke = new XElement(ns + "trace");
                        var strokeValue = string.Empty;
                        stroke.SetAttributeValue(XNamespace.Xml + "id", $"t{strokePoints[0].Number}");
                        var timeOffset = strokePoints[0].TimeStamp - timeOrigin;
                        stroke.SetAttributeValue("timeOffset", timeOffset);
                        foreach (var strokePoint in strokePoints)
                        {
                            strokeValue += $"{strokePoint.X} {strokePoint.Y} {strokePoint.Pressure} {strokePoint.TimeStamp - (timeOrigin + timeOffset)} {strokePoint.TangentialPressure} {strokePoint.Width} {strokePoint.Height} {strokePoint.TiltX} {strokePoint.TiltY} {(strokePoint.Twist == 0 ? 0 : (360 - strokePoint.Twist))}, ";
                        }
                        stroke.Value = strokeValue.Substring(0, strokeValue.Length - 2);
                        traceGroup.Add(stroke);
                    }

                    timeOrigin += 1616060000000;
                    var timestamp = document.Descendants(ns + "timestamp").First();
                    timestamp.SetAttributeValue("time", timeOrigin);

                    using var writer = XmlWriter.Create($"Data/InkMLs/Dataset/Writepads/{folderName}/{writepad.Type}/{writepad.Id}.inkml", new XmlWriterSettings { Async = true, Indent = true });
                    await document.SaveAsync(writer, default).ConfigureAwait(false);
                }
            }

            return count;
        }

        public async Task<int> ExportWriters()
        {
            IQueryable<ApplicationUser> query = _context.Users;
            var count = await query.CountAsync().ConfigureAwait(false);
            if (count == 0)
            {
                return count;
            }
            var partsRange = 200;
            var partsCount = Math.Ceiling((double)count / partsRange);

            for (int p = 0; p < partsCount; p++)
            {
                var users = await query
                    .OrderBy(u => u.Id)
                    .Skip(p * partsRange)
                    .Take(partsRange)
                    .ToListAsync().ConfigureAwait(false);

                foreach (var user in users)
                {
                    await Task.Delay(100).ConfigureAwait(false);

                    var document = new XDocument(WriterTemplate);

                    document.Root.SetAttributeValue("id", $"urn:uuid:{user.Id}");

                    if (user.Sex is null)
                    {
                        document.Root.Element("sex").Remove();
                    }
                    else
                    {
                        document.Root.SetElementValue("sex", user.Sex);
                    }
                    if (user.Education is null)
                    {
                        document.Root.Element("education").Remove();
                    }
                    else
                    {
                        document.Root.SetElementValue("education", user.Education);
                    }
                    if (user.BirthYear is null)
                    {
                        document.Root.Element("birthYear").Remove();
                    }
                    else
                    {
                        document.Root.SetElementValue("birthYear", user.BirthYear);
                    }
                    document.Root.SetElementValue("handedness", user.Handedness);

                    using var writer = XmlWriter.Create($"Data/InkMLs/Dataset/Writers/{user.Id}.xml", new XmlWriterSettings { Async = true, Indent = true });
                    await document.SaveAsync(writer, default).ConfigureAwait(false);
                }
            }

            return count;
        }

        public async Task<int> ExportGroundTruths(TextType type)
        {
            IQueryable<Text> query = _context.Text
                .Where(t => t.Type == type);
            var count = await query.CountAsync().ConfigureAwait(false);
            if (count == 0)
            {
                return count;
            }
            var partsRange = 200;
            var partsCount = Math.Ceiling((double)count / partsRange);

            Directory.CreateDirectory($"Data/InkMLs/Dataset/GroundTruths/{type}");

            for (int p = 0; p < partsCount; p++)
            {
                await Task.Delay(100).ConfigureAwait(false);

                var texts = await query
                    .OrderBy(t => t.Id)
                    .Skip(p * partsRange)
                    .Take(partsRange)
                    .ToListAsync().ConfigureAwait(false);

                var document = new XDocument(GTTemplate);

                document.Root.SetAttributeValue("type", type);

                foreach (var text in texts)
                {
                    var textElement = new XElement("text");

                    textElement.SetAttributeValue("id", text.Id);

                    var content = text.Content;
                    if (text.Type == TextType.Text)
                    {
                        textElement.SetElementValue("content", content);
                    }
                    else
                    {
                        var contentElement = new XElement("content");
                        foreach (var line in content.Split("\n"))
                        {
                            contentElement.Add(new XElement("line", line));
                        }
                        textElement.Add(contentElement);
                    }

                    document.Root.Add(textElement);
                }

                using var writer = XmlWriter.Create($"Data/InkMLs/Dataset/GroundTruths/{type}/{p}.xml", new XmlWriterSettings { Async = true, Indent = true });
                await document.SaveAsync(writer, default).ConfigureAwait(false);
            }

            return count;
        }
    }
}
