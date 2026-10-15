import { TreeDomain, liveConcepts, liveTopics } from './api/knowledge-tree.models';

export function domainConceptCount(domain: TreeDomain): number {
  return liveTopics(domain).reduce((n, t) => n + liveConcepts(t).length, 0);
}

/** "01" … "05". */
export function pad(position: number): string {
  return String(position).padStart(2, '0');
}
