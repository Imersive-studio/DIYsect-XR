// Message names, limits and the dissection RULES, shared by server and clients.
// Rule of thumb for the whole project: the LLM-free "referee" lives here and on the
// server. Clients may pre-check a rule for instant feedback, but the server decides.
//
// Full message shapes are documented in docs/PROTOCOL.md. If you change anything
// here, tell the other two teammates in the group chat first.

import { PARTS, PART_BY_ID } from './parts.js';

export const ROLES = { TEACHER: 'teacher', LEARNER: 'learner', AUDIENCE: 'audience' };

// client -> server
export const C2S = {
  JOIN: 'join',
  POSE: 'pose',            // teacher/learner: head + hands, in frog space
  POINT: 'point',          // teacher/learner: which part the ray is on (or null)
  REMOVE: 'removePart',    // teacher/learner
  RESTORE: 'restorePart',  // teacher/learner
  GUIDE: 'guide',          // teacher: a guide line (list of frog-space points) or null
  ASK: 'ask',              // audience: raise hand, tied to a touched part + a view
  ANSWER: 'answer',        // teacher: mark a question answered
  ATTENTION: 'attention',  // audience: which part I am looking at (awareness dots)
};

// server -> client
export const S2C = {
  WELCOME: 'welcome',
  STATE: 'state',          // removed parts, guide line, questions
  ROSTER: 'roster',
  POSE: 'pose',            // relayed pose of teacher/learner
  POINT: 'point',          // relayed pointer target of teacher/learner
  ATTENTION: 'attention',  // aggregated counts per part
  ERROR: 'error',
};

export const ROOM_CODE_RE = /^[A-Z0-9]{4}$/;

export const LIMITS = {
  maxText: 280,
  maxName: 24,
  maxGuidePoints: 64,
  maxQuestions: 100,
  poseMinIntervalMs: 40,
  askMinIntervalMs: 2000,
};

const label = (id) => PART_BY_ID[id]?.label ?? id;

/** Can this part be taken out right now? `removed` is a Set of part ids. */
export function canRemove(removed, id) {
  const part = PART_BY_ID[id];
  if (!part) return { ok: false, reason: 'Unknown part' };
  if (!part.removable) return { ok: false, reason: `${part.label} cannot be removed` };
  if (removed.has(id)) return { ok: false, reason: `${part.label} is already removed` };
  const blockers = part.coveredBy.filter((c) => !removed.has(c));
  if (blockers.length) {
    return { ok: false, reason: `Remove ${blockers.map(label).join(' and ')} first` };
  }
  return { ok: true };
}

/** Can this part be put back right now? Not while something it covers is still out. */
export function canRestore(removed, id) {
  const part = PART_BY_ID[id];
  if (!part) return { ok: false, reason: 'Unknown part' };
  if (!removed.has(id)) return { ok: false, reason: `${part.label} is not removed` };
  const stillOut = PARTS.filter((p) => p.coveredBy.includes(id) && removed.has(p.id));
  if (stillOut.length) {
    return { ok: false, reason: `Put back ${stillOut.map((p) => p.label).join(', ')} first` };
  }
  return { ok: true };
}
