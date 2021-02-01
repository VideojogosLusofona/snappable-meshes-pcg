/*
 * Copyright 2021 TrinityGenerator_Standalone contributors
 * (https://github.com/RafaelCS-Aula/TrinityGenerator_Standalone)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using UnityEngine;
using TrinityGen;

public class GenerationAnimator : MonoBehaviour
{

    [SerializeField] private float moveIntoPlaceTime;
    [SerializeField] private float height;
     [SerializeField] private float waitBetweenPieces;


    public void AnimateConstruction(ArenaPiece[] pieces)
    {
    
       StartCoroutine(MoveBlock(pieces));

        
    }


    public IEnumerator MoveBlock(ArenaPiece[] blocks)
    {

        for(int i = 1; i < blocks.Length; i++)
            blocks[i].gameObject.SetActive(false);

        for(int i = 1; i < blocks.Length; i++)
        {

            ArenaPiece current = blocks[i];
            Vector3 finalPosition = current.transform.position;

            Vector3 currentPosition = current.transform.position;
            currentPosition.y -= height;
            current.transform.position = currentPosition;
            current.gameObject.SetActive(true);
            float t = 0;
            Vector3 diff = finalPosition - current.transform.position;


            while(t < moveIntoPlaceTime)
            {
                current.transform.position = currentPosition +  diff * (t / moveIntoPlaceTime);
                t += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(waitBetweenPieces);
        }

    }
    public IEnumerator MovePiece(ArenaPiece toMove, Vector3 currentLoc, Vector3 targetLoc)
    {
        float t = 0;
        Vector3 diff = targetLoc - toMove.transform.position;


        while(t <= moveIntoPlaceTime)
        {
            toMove.transform.position = currentLoc +  diff * (t / moveIntoPlaceTime);
            t += Time.deltaTime;
            yield return null;
        }
        
    }

    private IEnumerator WaitTime() 
    {

        yield return new WaitForSeconds(waitBetweenPieces);
    } 

}
