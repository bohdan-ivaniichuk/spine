import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { TitleStrategy, provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { I18nTitleStrategy } from './shared/i18n/i18n-title.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch()),
    provideRouter(routes),
    { provide: TitleStrategy, useClass: I18nTitleStrategy },
  ],
};
