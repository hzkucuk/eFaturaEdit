<!--
  Kök layout — temayı BELGE KÖKÜNE (<html>) uygular.

  Neden gerekli: tema sınıfı daha önce yalnızca +page.svelte içindeki `.app`
  div'ine veriliyordu. Bu yüzden (a) o div'in DIŞINDA render edilen modallar,
  (b) ayrı bir route olan Ayarlar sayfası ve (c) kendi stil kapsamı olan AI
  paneli koyu temayı hiç almıyordu. `<html class="dark">` ile tema tüm
  route'lara ve tüm bileşenlere ulaşır (`:global(html.dark) ...`).
-->
<script lang="ts">
  import { settings, themeKind } from '$lib/settings.svelte';
  import { browser } from '$app/environment';

  let { children } = $props();

  $effect(() => {
    if (!browser) return;
    const dark = themeKind(settings.theme) === 'dark';
    document.documentElement.classList.toggle('dark', dark);
  });
</script>

{@render children()}

<style>
  /* Uygulama kabuğu: koyu temada belge arka planı da koyu olsun (aksi halde
     route geçişlerinde/kaydırmada beyaz kenarlar görünür). */
  :global(html.dark),
  :global(html.dark body) {
    background: #1a1a1a;
    color: #e6e6e6;
  }
</style>
