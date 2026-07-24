import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Login } from './login';
import { FormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login, FormsModule, RouterTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty login data', () => {
    expect(component.loginData().email).toBe('');
    expect(component.loginData().password).toBe('');
    expect(component.loginData().remember).toBe(false);
  });

  it('should toggle password visibility', () => {
    expect(component.showPw()).toBe(false);
    component.showPw.set(true);
    expect(component.showPw()).toBe(true);
  });

  it('should set isLoading to true on submit', () => {
    component.loginData.set({ email: 'test@test.de', password: 'password123', remember: false });
    component.onLogin();
    expect(component.isLoading()).toBe(true);
  });

  it('should clear errorMsg on submit', () => {
    component.errorMsg.set('Vorheriger Fehler');
    component.onLogin();
    expect(component.errorMsg()).toBe('');
  });

  it('should track email focus state', () => {
    expect(component.emailFocused()).toBe(false);
    component.emailFocused.set(true);
    expect(component.emailFocused()).toBe(true);
  });

  it('should track password focus state', () => {
    expect(component.pwFocused()).toBe(false);
    component.pwFocused.set(true);
    expect(component.pwFocused()).toBe(true);
  });

  it('should have specimen data defined', () => {
    expect(component.specimens().length).toBe(3);
    expect(component.specimens()[0].icon).toBe('🦋');
  });

  it('should have panel stats defined', () => {
    expect(component.panelStats().length).toBe(3);
  });
});
