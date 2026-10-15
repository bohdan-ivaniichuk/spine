import { ReviewScope, scopeLabel, toScopeQuery } from './review.models';

const conceptScope: ReviewScope = {
  type: 'concept',
  domain: 'dotnet',
  topic: 'clr-memory',
  concept: 'gc-modes',
  domainTitle: 'C# & .NET Runtime',
  topicTitle: 'Memory & GC',
  conceptTitle: 'GC modes',
};

describe('review scope helpers', () => {
  it('labels a concept scope as domain › topic › concept', () => {
    expect(scopeLabel(conceptScope)).toBe('C# & .NET Runtime › Memory & GC › GC modes');
  });

  it('turns a concept scope back into a request ("Review again")', () => {
    expect(toScopeQuery(conceptScope)).toEqual({
      scope: 'concept',
      domain: 'dotnet',
      topic: 'clr-memory',
      concept: 'gc-modes',
    });
  });

  it('keeps topic and domain scopes free of a concept', () => {
    expect(
      toScopeQuery({ ...conceptScope, type: 'topic', concept: null, conceptTitle: null }),
    ).toEqual({ scope: 'topic', domain: 'dotnet', topic: 'clr-memory' });
    expect(toScopeQuery({ ...conceptScope, type: 'domain', topic: null, concept: null })).toEqual({
      scope: 'domain',
      domain: 'dotnet',
    });
  });
});
