import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth-interceptor';
import { demoBackendInterceptor } from './demo/demo-backend.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // demoBackendInterceptor only answers requests when environment.demo is true
    provideHttpClient(withInterceptors([authInterceptor, demoBackendInterceptor]))
  ]
};
