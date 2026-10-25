using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class PdfActivityRow { public string name; public bool completed; public int errors; }

public class PdfReportData
{
    public string name, email, simulation, date, timeUsed, completionStatus, feedback;
    public bool competent;
    public int activitiesDone, activitiesTotal, overall, safety, errors;
    public List<PdfActivityRow> activities = new List<PdfActivityRow>();
}

public static class EvidencePdf
{
    const float PW = 595f, PH = 842f, M = 45f, CW = PW - 2 * M;
    const int GOOD = 0x1FA37A, BAD = 0xD64545, DARK = 0x1B2733, GRAY = 0x6B7785,
              LIGHT = 0xF3F5F7, LINE = 0xE6EAEE;

    // Helvetica character widths for ASCII 32..126 (per 1000 units)
    static readonly int[] Wd = {
        278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,
        556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,
        1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
        667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,
        333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,
        556,556,333,500,278,556,500,722,500,500,500,334,260,334,584 };

    static float Width(string s, float size, bool bold)
    {
        float w = 0;
        foreach (char c in s ?? "") w += (c >= 32 && c <= 126) ? Wd[c - 32] : 556;
        return w * size / 1000f * (bold ? 1.06f : 1f);
    }

    static string Clean(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s ?? "") sb.Append(c >= 32 && c <= 126 ? c : '?');
        return sb.ToString();
    }

    static string Fit(string s, float maxW, float size, bool bold)
    {
        s = s ?? "";
        if (Width(s, size, bold) <= maxW) return s;
        while (s.Length > 1 && Width(s + "...", size, bold) > maxW) s = s.Substring(0, s.Length - 1);
        return s + "...";
    }

    static List<string> Wrap(string text, float maxW, float size)
    {
        var lines = new List<string>();
        string cur = "";
        foreach (string word in (text ?? "").Replace("\n", " ").Split(' '))
        {
            if (word.Length == 0) continue;
            string trial = cur.Length == 0 ? word : cur + " " + word;
            if (Width(trial, size, false) > maxW && cur.Length > 0) { lines.Add(cur); cur = word; }
            else cur = trial;
        }
        if (cur.Length > 0) lines.Add(cur);
        return lines;
    }

    sealed class Page
    {
        public readonly StringBuilder C = new StringBuilder();
        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        static string Col(int h) => F(((h >> 16) & 255) / 255f) + " " + F(((h >> 8) & 255) / 255f) + " " + F((h & 255) / 255f);
        static string Esc(string s) => Clean(s).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        static float Y(float top) => PH - top;

        public void Rect(float x, float top, float w, float h, int hex)
            => C.Append($"{Col(hex)} rg {F(x)} {F(Y(top + h))} {F(w)} {F(h)} re f\n");

        public void RoundRect(float x, float top, float w, float h, float r, int hex)
        {
            float b = Y(top + h), t = b + h, R = x + w, k = r * 0.5523f;
            C.Append($"{Col(hex)} rg {F(x + r)} {F(b)} m {F(R - r)} {F(b)} l " +
                     $"{F(R - r + k)} {F(b)} {F(R)} {F(b + r - k)} {F(R)} {F(b + r)} c " +
                     $"{F(R)} {F(t - r)} l " +
                     $"{F(R)} {F(t - r + k)} {F(R - r + k)} {F(t)} {F(R - r)} {F(t)} c " +
                     $"{F(x + r)} {F(t)} l " +
                     $"{F(x + r - k)} {F(t)} {F(x)} {F(t - r + k)} {F(x)} {F(t - r)} c " +
                     $"{F(x)} {F(b + r)} l " +
                     $"{F(x)} {F(b + r - k)} {F(x + r - k)} {F(b)} {F(x + r)} {F(b)} c f\n");
        }

        public void Text(string s, float x, float baseTop, float size, bool bold, int hex)
            => C.Append($"BT /{(bold ? "F2" : "F1")} {F(size)} Tf {Col(hex)} rg {F(x)} {F(Y(baseTop))} Td ({Esc(s)}) Tj ET\n");

        public void TextRight(string s, float xRight, float baseTop, float size, bool bold, int hex)
            => Text(s, xRight - Width(s, size, bold), baseTop, size, bold, hex);

        public void HLine(float x1, float x2, float top, int hex, float lw)
            => C.Append($"{Col(hex)} RG {F(lw)} w {F(x1)} {F(Y(top))} m {F(x2)} {F(Y(top))} l S\n");

        public void Image(float x, float top, float w, float h)
            => C.Append($"q {F(w)} 0 0 {F(h)} {F(x)} {F(Y(top + h))} cm /Im1 Do Q\n");
    }

    public static byte[] Build(PdfReportData r, byte[] logoJpg, int logoW, int logoH, int brandHex)
    {
        bool hasLogo = logoJpg != null && logoJpg.Length > 0 && logoW > 0 && logoH > 0;
        var p = new Page();
        int resultColor = r.competent ? GOOD : BAD;

        float Section(string title, float top)
        {
            p.Text(title, M, top, 10, true, brandHex);
            p.HLine(M, PW - M, top + 7, brandHex, 1f);
            return top + 26;
        }
        float Row(string label, string value, float top, int color = DARK)
        {
            p.Text(label, M, top, 10, false, GRAY);
            p.Text(Fit(value, CW - 120, 11, true), M + 120, top, 11, true, color);
            return top + 20;
        }

        // Header banner
        p.Rect(0, 0, PW, 105, brandHex);
        float tx = M;
        if (hasLogo)
        {
            float lh = 64, lw = lh * logoW / logoH;
            p.Image(M, 20, lw, lh);
            tx = M + lw + 18;
        }
        p.Text("Skill-Proof", tx, 56, 26, true, 0xFFFFFF);
        p.Text("Skill Evidence Report", tx, 78, 13, false, 0xC9D6E2);
        p.TextRight("Issued " + DateTime.Now.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                    PW - M, 56, 10, false, 0xC9D6E2);

        // Result badge and overall score
        string badge = r.competent ? "COMPETENCY DEMONSTRATED" : "COMPETENCY NOT DEMONSTRATED";
        float bw = Width(badge, 12, true) + 40;
        p.RoundRect(M, 128, bw, 32, 16, resultColor);
        p.Text(badge, M + 20, 148, 12, true, 0xFFFFFF);
        p.TextRight("OVERALL SCORE", PW - M, 134, 9, true, GRAY);
        p.TextRight(r.overall + "%", PW - M, 168, 32, true, resultColor);

        // Candidate
        float y = Section("CANDIDATE", 196);
        y = Row("Name", r.name, y);
        y = Row("Email", r.email, y);

        // Simulation
        y = Section("SIMULATION", y + 18);
        y = Row("Simulation", r.simulation, y);
        y = Row("Date", r.date, y);
        y = Row("Status", r.completionStatus, y, r.completionStatus == "Completed" ? GOOD : BAD);

        // Stat cards
        float cardTop = y - 6, cw = 118f, gap = (CW - 4 * cw) / 3f;
        string[] labels = { "TIME USED", "ACTIVITIES", "SAFETY SCORE", "ERRORS" };
        string[] values = { r.timeUsed, r.activitiesDone + "/" + r.activitiesTotal, r.safety + "%", r.errors.ToString() };
        for (int i = 0; i < 4; i++)
        {
            float cx = M + i * (cw + gap);
            p.RoundRect(cx, cardTop, cw, 58, 8, LIGHT);
            p.Text(labels[i], cx + 12, cardTop + 20, 8, true, GRAY);
            p.Text(values[i], cx + 12, cardTop + 44, 20, true, DARK);
        }

        // Activities table
        float secTop = cardTop + 58 + 24;
        p.Text("ACTIVITIES", M, secTop, 10, true, brandHex);
        p.HLine(M, PW - M, secTop + 7, brandHex, 1f);
        float hdrTop = secTop + 16;
        p.Rect(M, hdrTop, CW, 22, LIGHT);
        p.Text("#", M + 10, hdrTop + 15, 8, true, GRAY);
        p.Text("ACTIVITY", M + 36, hdrTop + 15, 8, true, GRAY);
        p.Text("STATUS", M + 350, hdrTop + 15, 8, true, GRAY);
        p.TextRight("ERRORS", PW - M - 10, hdrTop + 15, 8, true, GRAY);

        float rowH = r.activities.Count > 6 ? 22f : 26f;
        float rowTop = hdrTop + 22;
        for (int i = 0; i < r.activities.Count; i++)
        {
            var act = r.activities[i];
            float bl = rowTop + rowH - 9;
            p.Text((i + 1).ToString(), M + 10, bl, 10, false, GRAY);
            p.Text(Fit(act.name, 300, 10, false), M + 36, bl, 10, false, DARK);
            p.Text(act.completed ? "Completed" : "Not completed", M + 350, bl, 10, true, act.completed ? GOOD : BAD);
            p.TextRight(act.errors > 0 ? act.errors.ToString() : "-", PW - M - 10, bl, 10, act.errors > 0, act.errors > 0 ? BAD : GRAY);
            rowTop += rowH;
            p.HLine(M, PW - M, rowTop, LINE, 0.6f);
        }

        // Feedback
        if (!string.IsNullOrEmpty(r.feedback))
        {
            float fTop = rowTop + 24;
            p.Text("FEEDBACK", M, fTop, 10, true, brandHex);
            p.HLine(M, PW - M, fTop + 7, brandHex, 1f);
            float boxTop = fTop + 18;
            var lines = Wrap(r.feedback, CW - 28, 10.5f);
            int maxLines = Math.Max(1, (int)((792 - boxTop - 20) / 15f));
            if (lines.Count > maxLines)
            {
                lines = lines.GetRange(0, maxLines);
                lines[maxLines - 1] = Fit(lines[maxLines - 1] + "...", CW - 28, 10.5f, false);
            }
            p.RoundRect(M, boxTop, CW, lines.Count * 15 + 20, 8, LIGHT);
            for (int i = 0; i < lines.Count; i++)
                p.Text(lines[i], M + 14, boxTop + 20 + i * 15, 10.5f, false, DARK);
        }

        // Footer
        p.HLine(M, PW - M, 800, LINE, 0.8f);
        p.Text("Generated automatically by the Skill-Proof simulation. Keep this report as proof of your skill demonstration.",
               M, 816, 8, false, GRAY);

        return Assemble(p.C.ToString(), hasLogo ? logoJpg : null, logoW, logoH);
    }

    static byte[] Assemble(string content, byte[] jpg, int jw, int jh)
    {
        var ms = new MemoryStream();
        var offs = new List<long>();
        void Put(string s) { var b = Encoding.ASCII.GetBytes(s); ms.Write(b, 0, b.Length); }
        void Begin() { offs.Add(ms.Position); Put(offs.Count + " 0 obj\n"); }

        Put("%PDF-1.4\n");
        Begin(); Put("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        Begin(); Put("<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        string xo = jpg != null ? " /XObject << /Im1 7 0 R >>" : "";
        Begin(); Put("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >>" + xo + " >> /Contents 6 0 R >>\nendobj\n");
        Begin(); Put("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        Begin(); Put("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");

        byte[] cb = Encoding.ASCII.GetBytes(content);
        Begin(); Put("<< /Length " + cb.Length + " >>\nstream\n"); ms.Write(cb, 0, cb.Length); Put("\nendstream\nendobj\n");

        if (jpg != null)
        {
            Begin();
            Put("<< /Type /XObject /Subtype /Image /Width " + jw + " /Height " + jh +
                " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + jpg.Length + " >>\nstream\n");
            ms.Write(jpg, 0, jpg.Length);
            Put("\nendstream\nendobj\n");
        }

        long xref = ms.Position;
        Put("xref\n0 " + (offs.Count + 1) + "\n0000000000 65535 f \n");
        foreach (long o in offs) Put(o.ToString("D10") + " 00000 n \n");
        Put("trailer\n<< /Size " + (offs.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
        return ms.ToArray();
    }
}