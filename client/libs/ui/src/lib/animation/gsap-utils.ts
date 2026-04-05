import { ElementRef, DestroyRef, inject } from '@angular/core';
import gsap from 'gsap';

/**
 * Spring animation — press effect for buttons/cards
 */
export function springPress(element: Element, scale = 0.96): gsap.core.Tween {
  return gsap.to(element, {
    scale,
    duration: 0.1,
    ease: 'power2.out',
    yoyo: true,
    repeat: 1,
  });
}

/**
 * Spring hover — subtle lift on hover
 */
export function springHover(element: Element, scale = 1.02): gsap.core.Tween {
  return gsap.to(element, {
    scale,
    duration: 0.3,
    ease: 'elastic.out(1, 0.5)',
  });
}

/**
 * Reset element transform to default
 */
export function resetTransform(element: Element): gsap.core.Tween {
  return gsap.to(element, {
    scale: 1,
    duration: 0.3,
    ease: 'elastic.out(1, 0.5)',
  });
}

/**
 * Fade in + slide up — entry animation for elements
 */
export function fadeInUp(
  element: Element | Element[],
  options: { duration?: number; delay?: number; y?: number } = {},
): gsap.core.Tween {
  const { duration = 0.5, delay = 0, y = 24 } = options;
  return gsap.fromTo(
    element,
    { opacity: 0, y },
    { opacity: 1, y: 0, duration, delay, ease: 'power3.out' },
  );
}

/**
 * Scale up — entry animation for modals/dialogs
 */
export function scaleIn(
  element: Element,
  options: { duration?: number; from?: number } = {},
): gsap.core.Tween {
  const { duration = 0.35, from = 0.95 } = options;
  return gsap.fromTo(
    element,
    { opacity: 0, scale: from },
    { opacity: 1, scale: 1, duration, ease: 'back.out(1.7)' },
  );
}

/**
 * Staggered fade in — for lists of items
 */
export function staggerFadeIn(
  elements: Element[] | NodeListOf<Element>,
  options: { stagger?: number; duration?: number; y?: number } = {},
): gsap.core.Tween {
  const { stagger = 0.05, duration = 0.4, y = 16 } = options;
  return gsap.fromTo(
    elements,
    { opacity: 0, y },
    { opacity: 1, y: 0, duration, stagger, ease: 'power2.out' },
  );
}

/**
 * Check if user prefers reduced motion
 */
export function prefersReducedMotion(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return false;
  }
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Safe animation — returns null if reduced motion is preferred
 */
export function safeAnimate(animationFn: () => gsap.core.Tween): gsap.core.Tween | null {
  if (prefersReducedMotion()) return null;
  return animationFn();
}

/**
 * Scroll-triggered reveal — fades in elements as they enter the viewport.
 * Uses Intersection Observer (no GSAP ScrollTrigger plugin needed).
 */
export function observeReveal(
  container: Element,
  selector: string,
  options: { threshold?: number; y?: number; duration?: number } = {},
): IntersectionObserver | null {
  if (prefersReducedMotion() || typeof IntersectionObserver === 'undefined') return null;

  const { threshold = 0.1, y = 24, duration = 0.5 } = options;

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          gsap.fromTo(
            entry.target,
            { opacity: 0, y },
            { opacity: 1, y: 0, duration, ease: 'power3.out' },
          );
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold },
  );

  container.querySelectorAll(selector).forEach((el) => {
    (el as HTMLElement).style.opacity = '0';
    observer.observe(el);
  });

  return observer;
}
