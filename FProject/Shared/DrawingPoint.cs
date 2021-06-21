using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FProject.Shared
{
    public class DrawingPoint
    {
        [JsonIgnore]
        public int WritepadId { get; set; }
        public int Number { get; set; }
        [JsonIgnore]
        public bool IsDeleted { get; set; }
        public PointType Type { get; set; }
        public double TimeStamp { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Pressure { get; set; }
        public float TangentialPressure { get; set; }
        public float TiltX { get; set; }
        public float TiltY { get; set; }
        public short Twist { get; set; }
    }

    public enum PointType
    {
        Middle,
        Starting,
        Ending
    }

    public class DeletedDrawing
    {
        public int StartingNumber { get; set; }
        public int EndingNumber { get; set; }
    }

    public class SavePointsRequestDTO
    {
        public DateTimeOffset LastModified { get; set; }
        public IEnumerable<DrawingPoint> NewPoints { get; set; }
        public IEnumerable<DeletedDrawing> DeletedDrawings { get; set; }
    }

    public class SavePointsResponseDTO
    {
        public DateTimeOffset LastModified { get; set; }
        public int LastSavedDrawingNumber { get; set; }
    }
}
