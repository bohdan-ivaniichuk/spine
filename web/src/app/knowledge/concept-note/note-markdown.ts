import hljs from 'highlight.js/lib/core';
import bash from 'highlight.js/lib/languages/bash';
import csharp from 'highlight.js/lib/languages/csharp';
import javascript from 'highlight.js/lib/languages/javascript';
import json from 'highlight.js/lib/languages/json';
import sql from 'highlight.js/lib/languages/sql';
import typescript from 'highlight.js/lib/languages/typescript';
import xml from 'highlight.js/lib/languages/xml';
import MarkdownIt from 'markdown-it';

/**
 * Concept-note Markdown → HTML (FR-003, FR-004). markdown-it with raw HTML disabled escapes any
 * inline or block HTML, and its link validator drops `javascript:`/`vbscript:`/`data:` URLs; the
 * result is bound with `[innerHTML]`, so Angular's sanitizer is a second layer. Only the languages
 * the content uses are registered with highlight.js core (this module lives in the lazy concept chunk).
 */

hljs.registerLanguage('csharp', csharp);
hljs.registerLanguage('typescript', typescript);
hljs.registerLanguage('javascript', javascript);
hljs.registerLanguage('sql', sql);
hljs.registerLanguage('json', json);
hljs.registerLanguage('xml', xml);
hljs.registerLanguage('bash', bash);
hljs.registerAliases(['html'], { languageName: 'xml' });
hljs.registerAliases(['shell', 'sh'], { languageName: 'bash' });

/** Display names for the code-block label, by highlight.js language name. */
const LANGUAGE_NAMES: Record<string, string> = {
  csharp: 'C#',
  typescript: 'TypeScript',
  javascript: 'JavaScript',
  sql: 'SQL',
  json: 'JSON',
  xml: 'HTML/XML',
  bash: 'Shell',
};

/** Localised accessible names for the scrollable regions the renderer emits. */
export interface NoteLabels {
  code: (language: string) => string;
  table: string;
  /** Appended (screen-reader only) to links that open in a new tab. */
  newTab: string;
}

const DEFAULT_LABELS: NoteLabels = {
  code: (language) => `${language} code`,
  table: 'Table',
  newTab: '(opens in a new tab)',
};

interface Env {
  labels: NoteLabels;
  /** One entry per open link: whether it is external (closing tag adds the new-tab hint). */
  links?: boolean[];
}

const md = new MarkdownIt({ html: false, linkify: false, typographer: false });

md.renderer.rules.fence = (tokens, idx, _options, env: Env) => {
  const token = tokens[idx];
  const requested = token.info.trim().split(/\s+/)[0]?.toLowerCase() ?? '';
  const language = requested && requested !== 'text' ? hljs.getLanguage(requested) : undefined;
  // Canonical registered name (aliases such as `html`, `sh`, `ts` resolve to the same definition).
  const key = language
    ? Object.keys(LANGUAGE_NAMES).find((k) => hljs.getLanguage(k) === language)
    : undefined;
  const body = key
    ? hljs.highlight(token.content, { language: key, ignoreIllegals: true }).value
    : md.utils.escapeHtml(token.content);
  const label = env.labels.code(key ? LANGUAGE_NAMES[key] : requested || 'text');
  const cls = key ? ` class="language-${key}"` : '';
  return (
    `<pre class="hljs" tabindex="0" role="region" aria-label="${md.utils.escapeHtml(label)}">` +
    `<code${cls}>${body}</code></pre>\n`
  );
};

md.renderer.rules['table_open'] = (_tokens, _idx, _options, env: Env) =>
  `<div class="note-table" tabindex="0" role="region" aria-label="${md.utils.escapeHtml(env.labels.table)}"><table>\n`;
md.renderer.rules['table_close'] = () => '</table></div>\n';

// Notes structure under the page's own h1 (the concept title): `#` becomes h2.
md.core.ruler.push('demote_h1', (state) => {
  for (const token of state.tokens) {
    if ((token.type === 'heading_open' || token.type === 'heading_close') && token.tag === 'h1') {
      token.tag = 'h2';
    }
  }
});

const renderLinkOpen = md.renderer.rules['link_open'];
md.renderer.rules['link_open'] = (tokens, idx, options, env: Env, self) => {
  const token = tokens[idx];
  const external = /^https?:\/\//i.test(token.attrGet('href') ?? '');
  if (external) {
    token.attrSet('target', '_blank');
    token.attrSet('rel', 'noopener noreferrer');
  }
  (env.links ??= []).push(external);
  return renderLinkOpen
    ? renderLinkOpen(tokens, idx, options, env, self)
    : self.renderToken(tokens, idx, options);
};

md.renderer.rules['link_close'] = (tokens, idx, options, env: Env, self) => {
  const external = env.links?.pop() ?? false;
  const hint = external
    ? `<span class="sr-only"> ${md.utils.escapeHtml(env.labels.newTab)}</span>`
    : '';
  return hint + self.renderToken(tokens, idx, options);
};

export function renderNote(markdown: string, labels: NoteLabels = DEFAULT_LABELS): string {
  const env: Env = { labels };
  return md.render(markdown, env);
}
