import { createI18n } from 'vue-i18n'
import en from './locales/en.json'
import pt from './locales/pt.json'

const defaultLocale = localStorage.getItem('adminshell-locale') || 'en'

export const i18n = createI18n({
  legacy: false,
  locale: defaultLocale,
  fallbackLocale: 'en',
  messages: {
    en,
    pt,
  },
})

export function setLocale(locale: string) {
  i18n.global.locale.value = locale as 'en' | 'pt'
  localStorage.setItem('adminshell-locale', locale)
}
