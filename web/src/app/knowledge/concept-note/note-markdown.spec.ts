import { renderNote } from './note-markdown';

/** Parses rendered HTML so assertions look at structure, not string formatting. */
function dom(markdown: string): HTMLElement {
  const div = document.createElement('div');
  div.innerHTML = renderNote(markdown);
  return div;
}

describe('renderNote', () => {
  it('renders headings, lists, emphasis, links and inline code', () => {
    const root = dom(
      [
        '## Generations',
        '',
        'The GC is **generational** and *compacting*; see `GC.Collect()`.',
        '',
        '- gen 0',
        '- gen 1',
        '',
        '1. mark',
        '2. sweep',
        '',
        '[Docs](https://learn.microsoft.com/dotnet/standard/garbage-collection/)',
      ].join('\n'),
    );

    expect(root.querySelector('h2')?.textContent).toBe('Generations');
    expect(root.querySelector('strong')?.textContent).toBe('generational');
    expect(root.querySelector('em')?.textContent).toBe('compacting');
    expect(root.querySelector('p code')?.textContent).toBe('GC.Collect()');
    expect(root.querySelectorAll('ul li')).toHaveLength(2);
    expect(root.querySelectorAll('ol li')).toHaveLength(2);
    const link = root.querySelector('a')!;
    expect(link.getAttribute('href')).toBe(
      'https://learn.microsoft.com/dotnet/standard/garbage-collection/',
    );
    expect(link.getAttribute('rel')).toContain('noopener');
  });

  it('wraps tables in a horizontally scrollable container', () => {
    const root = dom(
      '| Mode | Heaps |\n| --- | --- |\n| Workstation | 1 |\n| Server | 1 per core |',
    );

    const wrapper = root.querySelector('table')!.parentElement!;
    expect(wrapper.tagName).toBe('DIV');
    expect(wrapper.className).toContain('note-table');
    expect(root.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(root.querySelector('th')?.textContent).toBe('Mode');
  });

  it.each([
    ['csharp', 'public sealed class Foo { }'],
    ['typescript', 'const x: number = 1;'],
    ['javascript', 'const x = "a";'],
    ['sql', 'SELECT id FROM users WHERE id = 1;'],
    ['json', '{ "a": 1 }'],
    ['html', '<div class="a">x</div>'],
    ['xml', '<root a="1"/>'],
    ['bash', 'echo "hi" && ls -la'],
    ['shell', 'export A=1'],
    ['sh', 'echo "x"'],
  ])('highlights a %s fence with hljs spans inside a scrollable pre', (lang, code) => {
    const root = dom(`\`\`\`${lang}\n${code}\n\`\`\``);

    const pre = root.querySelector('pre')!;
    expect(pre).not.toBeNull();
    expect(pre.className).toContain('hljs');
    expect(pre.querySelector('code')!.textContent).toBe(`${code}\n`);
    expect(pre.querySelectorAll('[class^=hljs-]').length).toBeGreaterThan(0);
  });

  it('renders an unknown or text fence plain and escaped', () => {
    const root = dom('```brainfuck\n<b>++</b> & more\n```\n\n```text\nplain <i>x</i>\n```');

    const [unknown, text] = Array.from(root.querySelectorAll('pre'));
    expect(unknown.querySelector('b')).toBeNull();
    expect(unknown.textContent).toBe('<b>++</b> & more\n');
    expect(unknown.querySelectorAll('[class^=hljs-]')).toHaveLength(0);
    expect(text.querySelector('i')).toBeNull();
    expect(text.textContent).toBe('plain <i>x</i>\n');
  });

  describe('neutralises unsafe content (FR-004)', () => {
    it('escapes a <script> block instead of emitting it', () => {
      const html = renderNote('<script>alert(1)</script>\n\nText');
      const root = dom('<script>alert(1)</script>\n\nText');

      expect(root.querySelector('script')).toBeNull();
      expect(html).toContain('&lt;script&gt;');
    });

    it('escapes <img onerror> and inline HTML', () => {
      const root = dom('Before <img src=x onerror="alert(1)"> and <b onclick="x()">bold</b> after');

      expect(root.querySelector('img')).toBeNull();
      expect(root.querySelector('b')).toBeNull();
      expect(root.querySelector('[onerror],[onclick]')).toBeNull();
      expect(root.textContent).toContain('<img src=x onerror="alert(1)">');
    });

    it('does not turn javascript:, vbscript: or data: URLs into links', () => {
      const root = dom(
        '[a](javascript:alert(1)) [b](JAVASCRIPT:alert(1)) [c](vbscript:msgbox) [d](data:text/html;base64,PHNjcmlwdD4=)',
      );

      expect(root.querySelectorAll('a')).toHaveLength(0);
    });

    it('does not let a fence language break out of the class attribute', () => {
      const root = dom('```csharp" onmouseover="alert(1)\nvar x = 1;\n```');

      expect(root.querySelector('[onmouseover]')).toBeNull();
    });
  });

  it('demotes a level-1 heading to h2 (the page title is the only h1)', () => {
    const root = dom('# Overview\n\n## Details');

    expect(root.querySelector('h1')).toBeNull();
    expect(Array.from(root.querySelectorAll('h2')).map((h) => h.textContent)).toEqual([
      'Overview',
      'Details',
    ]);
  });

  it('adds a localised screen-reader hint to external links only', () => {
    const div = document.createElement('div');
    div.innerHTML = renderNote('[ext](https://example.com) and [int](/atlas/dotnet)', {
      code: (l) => l,
      table: 'Таблиця',
      newTab: '(відкривається в новій вкладці)',
    });

    const [ext, int] = Array.from(div.querySelectorAll('a'));
    expect(ext.getAttribute('target')).toBe('_blank');
    expect(ext.querySelector('.sr-only')?.textContent?.trim()).toBe(
      '(відкривається в новій вкладці)',
    );
    expect(int.getAttribute('target')).toBeNull();
    expect(int.querySelector('.sr-only')).toBeNull();
    expect(int.textContent).toBe('int');
  });
});
