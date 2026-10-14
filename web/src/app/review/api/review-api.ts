import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AnswerFeedback,
  ReviewOverview,
  ScopeQuery,
  Session,
  SessionSummary,
  StartSessionRequest,
} from './review.models';

const BASE = '/api/review';

/** `GET /api/review/overview` URL for a scope — a plain string so `httpResource` re-requests only on real changes. */
export function overviewUrl(query: ScopeQuery): string {
  let params = new HttpParams().set('scope', query.scope);
  if (query.domain) {
    params = params.set('domain', query.domain);
  }
  if (query.topic) {
    params = params.set('topic', query.topic);
  }
  return `${BASE}/overview?${params.toString()}`;
}

/** Best human-readable message from a ProblemDetails error (or anything else that was thrown). */
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as { title?: unknown; detail?: unknown } | null;
    if (problem && typeof problem === 'object') {
      const text = [problem.title, problem.detail].filter((x) => typeof x === 'string');
      if (text.length > 0) {
        return text.join(' — ');
      }
    }
    return error.status === 0 ? 'API unreachable' : `Request failed (${error.status})`;
  }
  return error instanceof Error ? error.message : 'Something went wrong';
}

/**
 * Thin promise-based client for the six review endpoints. The `SessionStore` owns the state;
 * reads that are a pure function of signals (the overview) use `httpResource` in the component instead.
 */
@Injectable({ providedIn: 'root' })
export class ReviewApi {
  private readonly http = inject(HttpClient);

  overview(query: ScopeQuery): Promise<ReviewOverview> {
    return firstValueFrom(this.http.get<ReviewOverview>(overviewUrl(query)));
  }

  start(request: StartSessionRequest): Promise<Session> {
    return firstValueFrom(this.http.post<Session>(`${BASE}/sessions`, request));
  }

  /** The open session, or `null` when the API answers 204. */
  async current(): Promise<Session | null> {
    const response = await firstValueFrom(
      this.http.get<Session>(`${BASE}/sessions/current`, { observe: 'response' }),
    );
    return response.status === 204 ? null : response.body;
  }

  get(id: string): Promise<Session> {
    return firstValueFrom(this.http.get<Session>(`${BASE}/sessions/${id}`));
  }

  answer(id: string, questionId: string, selectedIndex: number): Promise<AnswerFeedback> {
    return firstValueFrom(
      this.http.post<AnswerFeedback>(`${BASE}/sessions/${id}/answers`, {
        questionId,
        selectedIndex,
      }),
    );
  }

  finish(id: string): Promise<SessionSummary> {
    return firstValueFrom(this.http.post<SessionSummary>(`${BASE}/sessions/${id}/finish`, {}));
  }
}
