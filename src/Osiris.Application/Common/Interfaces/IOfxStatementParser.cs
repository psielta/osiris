using Osiris.Application.Common.Ofx;

namespace Osiris.Application.Common.Interfaces;

public interface IOfxStatementParser
{
    /// <summary>
    /// Parses the raw bytes of an OFX file (1.x SGML or 2.x XML) into a normalized statement.
    /// Tolerant by design: returns whatever transactions it can read and an empty list when the
    /// content is not a recognizable OFX statement. Never throws on malformed input.
    /// </summary>
    OfxStatement Parse(byte[] content);
}
