using UnityEngine;

namespace BulkoGame.Art
{
    // Production environment art. Legacy art remains available for unchanged collider geometry.
    [CreateAssetMenu(menuName = "Bulka: Sock Patrol/Environment Art Set")]
    public sealed class EnvironmentArtSet : ScriptableObject
    {
        public Sprite bed, wardrobe, bedsideTableLamp, lowBookShelf, bedroomRug;
        public Sprite sofa, coffeeTable, tvConsole, sideboard, armchair, livingRoomRug;
        public Sprite refrigerator, stoveOven, sinkCabinet, counterModule, diningTableChairs, kitchenRug, coffeeCounter;
        public Sprite bathtub, bathroomSinkCabinet, toilet, narrowCabinet, shower, bathroomRug, laundryBasket, accessoryShelf;
        public Sprite monsteraPlant, trailingPlant, tableLamp, books, framedLandscape, greenSlippers, sleepingCat;
        public Sprite sockBasketLow, sockBasketMedium, sockBasketFull;
        public Sprite sockCreamStripe, sockGreen, sockLeaf, sockOrangeDots, sockGreenCheck, sockHeart;
        public Sprite slowTrapArmed, slowTrapTriggered, slowTrapUsed;
    }
}
