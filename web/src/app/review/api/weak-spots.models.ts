import { ConceptReadiness } from '../../knowledge/api/readiness.models';

/**
 * `GET /api/review/weak-spots` (`specs/006-weak-spots/contracts/weak-spots-api.md`): the local
 * learner's weakest concepts and topics over a trailing window. Slugs and figures only — the client
 * joins titles from the knowledge tree; the payload does not depend on the language, so no `?lang=`.
 */

/** Figures shared by a concept and a topic row. */
export interface WeaknessFigures {
  /** Answers in the window. */
  answers: number;
  /** Wrong answers in the window. */
  wrong: number;
  /** `(answers − wrong) / answers`, 0..1. */
  accuracy: number;
  lastAnsweredAt: string;
  /** Questions whose most recent answer is wrong (not windowed): what a weak drill would contain. */
  weakQuestions: number;
}

export interface WeakConceptStats extends WeaknessFigures {
  domain: string;
  topic: string;
  concept: string;
  /** As `GET /api/review/readiness` reports it; `null` without schedulable questions. */
  readiness: ConceptReadiness | null;
}

export interface WeakTopicStats extends WeaknessFigures {
  domain: string;
  topic: string;
}

export interface WeakSpots {
  /** Start of the window (`now − windowDays`). */
  since: string;
  windowDays: number;
  /** Answers a concept or topic needs in the window to be ranked. */
  minAnswers: number;
  /** Weakest first, at most 20. */
  concepts: WeakConceptStats[];
  /** Weakest first, at most 10. */
  topics: WeakTopicStats[];
}

export const WEAK_SPOTS_URL = '/api/review/weak-spots';
