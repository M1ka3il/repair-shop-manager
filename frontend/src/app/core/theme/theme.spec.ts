import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    delete document.documentElement.dataset['theme'];
  });

  function create(): ThemeService {
    const service = TestBed.inject(ThemeService);
    TestBed.tick();
    return service;
  }

  it('follows the OS by default: no data-theme attribute, nothing stored', () => {
    const theme = create();

    expect(theme.choice()).toBe('system');
    expect(document.documentElement.dataset['theme']).toBeUndefined();
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
  });

  it('cycles System → Hell → Dunkel → System and remembers the choice', () => {
    const theme = create();

    theme.cycle();
    TestBed.tick();
    expect(document.documentElement.dataset['theme']).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');

    theme.cycle();
    TestBed.tick();
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(theme.nextLabel()).toBe('System');

    theme.cycle();
    TestBed.tick();
    expect(document.documentElement.dataset['theme']).toBeUndefined();
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
  });

  it('restores a stored choice and ignores garbage', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');
    expect(create().choice()).toBe('dark');
  });

  it('treats an unknown stored value as "system"', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'neon');
    expect(create().choice()).toBe('system');
  });
});
