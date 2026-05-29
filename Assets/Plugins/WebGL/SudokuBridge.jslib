mergeInto(LibraryManager.library, {
  // ブラウザの localStorage からプレイヤー名を取得
  GetPlayerNameFromBrowser: function () {
    var name = localStorage.getItem('jigsaw_player_name') || "";
    var bufferSize = lengthBytesUTF8(name) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(name, buffer, bufferSize);
    return buffer;
  },
  
  // ブラウザ側の window.onSudokuComplete にスコアを送信
  SendScoreToBrowser: function (difficultyStr, elapsedTime, playerNameStr) {
    var difficulty = UTF8ToString(difficultyStr);
    var name = UTF8ToString(playerNameStr);
    if (typeof window.onSudokuComplete === 'function') {
      window.onSudokuComplete(difficulty, elapsedTime, name);
    } else {
      console.warn("window.onSudokuComplete is not defined.");
    }
  }
});
