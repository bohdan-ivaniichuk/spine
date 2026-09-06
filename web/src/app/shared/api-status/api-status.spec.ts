import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApiStatus } from './api-status';

describe('ApiStatus', () => {
  let fixture: ComponentFixture<ApiStatus>;
  let http: HttpTestingController;

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const retryButton = () =>
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button');

  /** Creates the component and runs effects so `httpResource` issues its first request. */
  const create = () => {
    fixture = TestBed.createComponent(ApiStatus);
    TestBed.tick();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApiStatus],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  it('shows a checking state while the request is pending', () => {
    create();

    expect(text()).toContain('Checking API');
    expect(retryButton()).toBeNull();
    http.expectOne('/api/health').flush({ status: 'Healthy' });
  });

  it('shows "API: OK" when /api/health answers', async () => {
    create();
    http.expectOne('/api/health').flush({ status: 'Healthy' });
    await fixture.whenStable();

    expect(text()).toContain('API: OK');
    expect(retryButton()).toBeNull();
  });

  it('shows "API: unreachable" with a retry button when /api/health fails', async () => {
    create();
    http
      .expectOne('/api/health')
      .flush({ status: 'Unhealthy' }, { status: 500, statusText: 'Internal Server Error' });
    await fixture.whenStable();

    expect(text()).toContain('API: unreachable');
    expect(retryButton()).not.toBeNull();
  });

  it('re-requests /api/health when Retry is clicked', async () => {
    create();
    http.expectOne('/api/health').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();
    expect(text()).toContain('API: unreachable');

    retryButton()!.click();
    TestBed.tick();
    http.expectOne('/api/health').flush({ status: 'Healthy' });
    await fixture.whenStable();

    expect(text()).toContain('API: OK');
  });

  it('polls /api/health every 30 seconds and stops when destroyed', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    create();
    http.expectOne('/api/health').flush({ status: 'Healthy' });
    await fixture.whenStable();

    vi.advanceTimersByTime(30_000);
    TestBed.tick();
    http.expectOne('/api/health').flush({ status: 'Healthy' });
    await fixture.whenStable();
    expect(text()).toContain('API: OK');

    fixture.destroy();
    vi.advanceTimersByTime(60_000);
    http.expectNone('/api/health');
  });
});
