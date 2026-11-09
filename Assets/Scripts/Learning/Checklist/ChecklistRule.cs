using System;
using System.Collections.Generic;

namespace DIYsect.Learning
{
    /// <summary>
    /// The checklist order, as data: each row says "in this state, this event moves the checklist to that state".
    /// Anything not in the table is ignored, so an event that arrives out of order or twice changes nothing.
    /// A new step is a new row (and a new enum value), not a new code path.
    /// Pure logic with no Unity or network types, so it is easy to test.
    /// </summary>
    public sealed class ChecklistRule
    {
        public readonly struct Row
        {
            public Row(ChecklistState from, ChecklistEvent on, ChecklistState to, ChecklistResult result)
            {
                From = from;
                On = on;
                To = to;
                Result = result;
            }

            public ChecklistState From { get; }
            public ChecklistEvent On { get; }
            public ChecklistState To { get; }
            public ChecklistResult Result { get; }
        }

        /// <summary>The steps in the order the student does them, plus the two ways back (drop the organ, new body).</summary>
        public static ChecklistRule Default { get; } = new ChecklistRule(DefaultRows());

        private readonly Dictionary<(ChecklistState, ChecklistEvent), Row> _rows = new Dictionary<(ChecklistState, ChecklistEvent), Row>();

        public ChecklistRule(IEnumerable<Row> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            foreach (Row row in rows)
                _rows.Add((row.From, row.On), row);
        }

        /// <summary>Judges one event at the current state. <paramref name="next"/> equals <paramref name="current"/> when the result is Ignored.</summary>
        public ChecklistResult Evaluate(ChecklistState current, ChecklistEvent trigger, out ChecklistState next)
        {
            if (!_rows.TryGetValue((current, trigger), out Row row))
            {
                next = current;
                return ChecklistResult.Ignored;
            }
            next = row.To;
            return row.Result;
        }

        private static IEnumerable<Row> DefaultRows()
        {
            var rows = new List<Row>
            {
                new Row(ChecklistState.TraceIncision, ChecklistEvent.IncisionTraced, ChecklistState.OpenSkin, ChecklistResult.Advanced),
                new Row(ChecklistState.OpenSkin, ChecklistEvent.SkinOpened, ChecklistState.OpenRectus, ChecklistResult.Advanced),
                new Row(ChecklistState.OpenRectus, ChecklistEvent.RectusOpened, ChecklistState.LiftOmentum, ChecklistResult.Advanced),
                new Row(ChecklistState.LiftOmentum, ChecklistEvent.OmentumLifted, ChecklistState.LiftOrgan, ChecklistResult.Advanced),
                new Row(ChecklistState.LiftOrgan, ChecklistEvent.OrganLifted, ChecklistState.InspectOrgan, ChecklistResult.Advanced),
                new Row(ChecklistState.InspectOrgan, ChecklistEvent.OrganInspected, ChecklistState.PutOrganBack, ChecklistResult.Advanced),
                // Letting go before the inspection counts sends the student back to lifting an organ.
                new Row(ChecklistState.InspectOrgan, ChecklistEvent.OrganDropped, ChecklistState.LiftOrgan, ChecklistResult.Reverted),
                new Row(ChecklistState.PutOrganBack, ChecklistEvent.OrganPutBack, ChecklistState.Complete, ChecklistResult.Advanced),
            };
            // A fresh body starts the checklist again from any step except the first (where there is nothing to undo).
            for (ChecklistState state = ChecklistState.OpenSkin; state <= ChecklistState.Complete; state++)
                rows.Add(new Row(state, ChecklistEvent.BodyReset, ChecklistState.TraceIncision, ChecklistResult.Reset));
            return rows;
        }
    }
}
