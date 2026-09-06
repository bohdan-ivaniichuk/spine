import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { Theme } from './theme';

describe('Theme', () => {
  let theme: Theme;
  let root: HTMLElement;

  beforeEach(() => {
    localStorage.removeItem('interviewprep.theme');
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({});
    theme = TestBed.inject(Theme);
    root = TestBed.inject(DOCUMENT).documentElement;
    TestBed.tick();
  });

  it('follows the system by default (no data-theme attribute)', () => {
    expect(theme.preference()).toBe('system');
    expect(root.hasAttribute('data-theme')).toBe(false);
  });

  it('toggle sets an explicit theme on <html> and persists it', () => {
    const before = theme.effective();
    theme.toggle();
    TestBed.tick();
    const after = before === 'dark' ? 'light' : 'dark';
    expect(theme.effective()).toBe(after);
    expect(root.getAttribute('data-theme')).toBe(after);
    expect(localStorage.getItem('interviewprep.theme')).toBe(after);
  });

  it('restores a stored preference on start', () => {
    localStorage.setItem('interviewprep.theme', 'dark');
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const fresh = TestBed.inject(Theme);
    TestBed.tick();
    expect(fresh.preference()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
