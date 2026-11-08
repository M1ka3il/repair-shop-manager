import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signInForTest, signOutForTest } from '../testing/auth';
import { App } from './app';

describe('App shell', () => {
  async function render(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  const links = (el: HTMLElement) =>
    Array.from(el.querySelectorAll('nav a')).map((a) => a.textContent?.trim());

  afterEach(() => signOutForTest());

  it('shows the office everything, with user, role and sign-out', async () => {
    signInForTest('Office');
    const el = await render();

    expect(links(el)).toEqual(['Board', 'Kunden', 'Umsatzbericht']);
    expect(el.querySelector('a.new-order')?.getAttribute('href')).toBe('/orders/new');
    expect(el.querySelector('.user')?.textContent).toContain('buero');
    expect(el.querySelector('.role')?.textContent).toBe('Büro');
    expect(el.querySelector('.logout')).not.toBeNull();
  });

  it('shows the workshop only the board, without "new order"', async () => {
    signInForTest('Mechanic');
    const el = await render();

    expect(links(el)).toEqual(['Board']);
    expect(el.querySelector('a.new-order')).toBeNull();
    expect(el.querySelector('.role')?.textContent).toBe('Werkstatt');
  });

  it('shows no navigation before signing in', async () => {
    signOutForTest();
    const el = await render();

    expect(links(el)).toEqual([]);
    expect(el.querySelector('.logout')).toBeNull();
  });
});
