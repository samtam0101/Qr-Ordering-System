window.admin = {
    copyText: async function (text) {
      try {
        await navigator.clipboard.writeText(text);
        // optional toast:
        // alert("Link copied");
      } catch {
        // Fallback: create a temp input
        const i = document.createElement("input");
        i.value = text;
        document.body.appendChild(i);
        i.select();
        document.execCommand("copy");
        document.body.removeChild(i);
        // alert("Link copied");
      }
    },
    downloadDataUrl: function (filename, dataUrl) {
      const a = document.createElement("a");
      a.href = dataUrl;       // data:image/png;base64,....
      a.download = filename;  // e.g. my_restaurant_T1.png
      document.body.appendChild(a);
      a.click();
      a.remove();
    }
  };
  