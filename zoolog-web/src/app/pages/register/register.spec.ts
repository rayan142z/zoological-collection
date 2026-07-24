import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Register } from './register';
import { FormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register, FormsModule, RouterTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty registration data', () => {
    expect(component.regData().username).toBe('');
    expect(component.regData().email).toBe('');
    expect(component.regData().password).toBe('');
    expect(component.regData().terms).toBe(false);
  });

  it('should toggle password visibility', () => {
    expect(component.showPw()).toBe(false);
    component.showPw.set(true);
    expect(component.showPw()).toBe(true);
  });

  it('should validate username correctly', () => {
    component.regData.set({ ...component.regData(), username: 'ab' });
    component.validateUsername();
    expect(component.usernameOk()).toBe(false);

    component.regData.set({ ...component.regData(), username: 'abc' });
    component.validateUsername();
    expect(component.usernameOk()).toBe(true);
  });

  describe('checkStrength', () => {
    it('should rate short password as weak', () => {
      component.regData.set({ ...component.regData(), password: 'abc' });
      expect(component.strengthLevel()).toBe('weak');
      expect(component.strengthPct()).toBe(0);
    });

    it('should rate medium password as fair', () => {
      component.regData.set({ ...component.regData(), password: 'Abcdefgh' });
      expect(component.strengthLevel()).toBe('fair');
      expect(component.strengthPct()).toBe(45);
    });

    it('should rate complex password as strong', () => {
      component.regData.set({ ...component.regData(), password: 'Abcdefgh1234' });
      expect(component.strengthLevel()).toBe('strong');
      expect(component.strengthPct()).toBe(100); // Adjusted threshold logic
    });

    it('should set strength text correctly', () => {
      component.regData.set({ ...component.regData(), password: 'abc' });
      expect(component.strengthText()).toBe('Schwach');

      component.regData.set({ ...component.regData(), password: 'Abcdefgh' });
      expect(component.strengthText()).toBe('Mittel');

      component.regData.set({ ...component.regData(), password: 'Abcdefgh1234' });
      expect(component.strengthText()).toBe('Stark');
    });
  });

  it('should not submit if terms not accepted', () => {
    component.regData.set({ ...component.regData(), terms: false });
    component.onRegister();
  });

  it('should set isLoading when terms are accepted', () => {
    component.regData.set({ ...component.regData(), terms: true });
    component.onRegister();
    expect(component.isLoading()).toBe(true);
  });

  it('should clear errorMsg on submit', () => {
    component.regData.set({ ...component.regData(), terms: true });
    component.errorMsg.set('Vorheriger Fehler');
    component.onRegister();
    expect(component.errorMsg()).toBe('');
  });

  it('should have features list defined', () => {
    expect(component.features().length).toBe(4);
    expect(component.features()[0].title).toBe('Sammlung verwalten');
  });
});
